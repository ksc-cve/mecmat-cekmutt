using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Finance;

public sealed record AccountNode(
    string Id, string Code, string NameTh, string? NameEn, string? ParentAccountId, AccountKind Kind, string? ProjectId,
    BudgetCategory? BudgetCategory, string? CustodianPersonId, bool IsLeaf, long Balance, long Committed, long Available,
    long Budget, long SpentThisYear, bool CanRequest, bool CanEdit);

public sealed class FinanceService(AppDbContext db, CurrentUser cu)
{
    private static readonly MoneyRequestStatus[] Open =
        [MoneyRequestStatus.Submitted, MoneyRequestStatus.InApproval, MoneyRequestStatus.Approved];

    public static bool DrawsMoney(MoneyRequestKind k) => k is not MoneyRequestKind.AdvanceClearance;

    /// <summary>Accounts the user may raise requests against: everything with Finance Create on All;
    /// otherwise accounts they look after and accounts of projects they belong to, with sub-accounts.</summary>
    public async Task<HashSet<string>> RequestableAsync(Scope s)
    {
        var accounts = await db.Set<Account>().AsNoTracking().Where(a => a.RecordStatus == RecordStatus.Active)
            .Select(a => new { a.Id, a.ParentAccountId, a.ProjectId }).ToListAsync();
        if (cu.CanAll(Modules.Finance, PermAction.Create)) return accounts.Select(a => a.Id).ToHashSet();
        if (!cu.CanAny(Modules.Finance, PermAction.Create)) return [];
        var roots = accounts.Where(a => s.AccountIds.Contains(a.Id) || (a.ProjectId != null && s.ProjectIds.Contains(a.ProjectId))).Select(a => a.Id);
        var set = new HashSet<string>();
        var stack = new Stack<string>(roots);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!set.Add(id)) continue;
            foreach (var c in accounts.Where(a => a.ParentAccountId == id)) stack.Push(c.Id);
        }
        return set;
    }

    public async Task<Dictionary<string, long>> LeafBalancesAsync()
    {
        var into = await db.Set<LedgerEntry>().Where(l => l.ToAccountId != null)
            .GroupBy(l => l.ToAccountId!).Select(g => new { g.Key, Sum = g.Sum(l => l.Amount) }).ToListAsync();
        var outOf = await db.Set<LedgerEntry>().Where(l => l.FromAccountId != null)
            .GroupBy(l => l.FromAccountId!).Select(g => new { g.Key, Sum = g.Sum(l => l.Amount) }).ToListAsync();
        var bal = new Dictionary<string, long>();
        foreach (var r in into) bal[r.Key] = bal.GetValueOrDefault(r.Key) + r.Sum;
        foreach (var r in outOf) bal[r.Key] = bal.GetValueOrDefault(r.Key) - r.Sum;
        return bal;
    }

    /// <summary>Money promised but not yet booked: open requests drawing on each account.</summary>
    public async Task<Dictionary<string, long>> CommittedAsync(string? exceptRequestId = null)
    {
        var rows = await db.Set<MoneyRequest>().AsNoTracking()
            .Where(r => Open.Contains(r.Status) && r.Id != exceptRequestId && r.RecordStatus == RecordStatus.Active)
            .Select(r => new { r.AccountId, r.Kind, r.TotalAmount, r.RelatedAdvanceId }).ToListAsync();
        var advances = await db.Set<MoneyRequest>().AsNoTracking().Where(r => r.Kind == MoneyRequestKind.Advance)
            .ToDictionaryAsync(r => r.Id, r => r.TotalAmount);
        var map = new Dictionary<string, long>();
        foreach (var r in rows)
        {
            var draw = DrawsMoney(r.Kind) ? r.TotalAmount
                : Math.Max(0, r.TotalAmount - (r.RelatedAdvanceId != null ? advances.GetValueOrDefault(r.RelatedAdvanceId) : 0));
            map[r.AccountId] = map.GetValueOrDefault(r.AccountId) + draw;
        }
        return map;
    }

    public async Task<List<AccountNode>> TreeAsync(Scope s)
    {
        var accounts = await db.Set<Account>().AsNoTracking().Where(a => a.RecordStatus != RecordStatus.Deleted)
            .OrderBy(a => a.Code).ToListAsync();
        var bal = await LeafBalancesAsync();
        var committed = await CommittedAsync();
        var fy = FinanceRules.FiscalYear(Clock.TodayBangkok());
        var budgets = await db.Set<AccountBudget>().AsNoTracking().Where(b => b.FiscalYear == fy && b.RecordStatus == RecordStatus.Active)
            .GroupBy(b => b.AccountId).Select(g => new { g.Key, Sum = g.Sum(b => b.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Sum);
        var (from, to) = (new DateOnly(fy - 1, 10, 1), new DateOnly(fy, 9, 30));
        var spent = await db.Set<LedgerEntry>().Where(l => l.FromAccountId != null && l.ToAccountId == null
                                                           && l.EntryDate >= from && l.EntryDate <= to)
            .GroupBy(l => l.FromAccountId!).Select(g => new { g.Key, Sum = g.Sum(l => l.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Sum);
        var returned = await db.Set<LedgerEntry>().Where(l => l.FromAccountId == null && l.ToAccountId != null
                                                              && (l.Kind == LedgerKind.AdvanceReturn || l.Kind == LedgerKind.Reversal)
                                                              && l.EntryDate >= from && l.EntryDate <= to)
            .GroupBy(l => l.ToAccountId!).Select(g => new { g.Key, Sum = g.Sum(l => l.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Sum);

        var requestable = await RequestableAsync(s);
        var children = accounts.ToLookup(a => a.ParentAccountId);
        var visibleAll = cu.CanAll(Modules.Finance, PermAction.View);
        var nodes = new List<AccountNode>();

        (long Bal, long Com, long Bud, long Spent) Roll(Account a)
        {
            var kids = children[a.Id].ToList();
            long b = bal.GetValueOrDefault(a.Id), c = committed.GetValueOrDefault(a.Id), bu = budgets.GetValueOrDefault(a.Id),
                sp = spent.GetValueOrDefault(a.Id) - returned.GetValueOrDefault(a.Id);
            foreach (var k in kids)
            {
                var r = Roll(k);
                b += r.Bal; c += r.Com; sp += r.Spent;
                // A parent's own budget line wins; otherwise it is the sum of what its children plan.
                if (!budgets.ContainsKey(a.Id)) bu += r.Bud;
            }
            if (visibleAll || s.AccountIds.Contains(a.Id) || requestable.Contains(a.Id))
                nodes.Add(new AccountNode(a.Id, a.Code, a.NameTh, a.NameEn, a.ParentAccountId, a.Kind, a.ProjectId, a.BudgetCategory,
                    a.CustodianPersonId, kids.Count == 0, b, c, b - c, bu, sp,
                    kids.Count == 0 && requestable.Contains(a.Id),
                    cu.Can(Modules.Finance, PermAction.Edit, s.AccountIds.Contains(a.Id))));
            return (b, c, bu, sp);
        }
        foreach (var root in children[null]) Roll(root);
        return nodes.OrderBy(n => n.Code).ToList();
    }

    public async Task<long> AvailableAsync(string accountId, string? exceptRequestId)
    {
        var bal = (await LeafBalancesAsync()).GetValueOrDefault(accountId);
        var com = (await CommittedAsync(exceptRequestId)).GetValueOrDefault(accountId);
        return bal - com;
    }

    public async Task<bool> IsLeafAsync(string accountId) =>
        !await db.Set<Account>().AnyAsync(a => a.ParentAccountId == accountId && a.RecordStatus != RecordStatus.Deleted);

    /// <summary>The project an account belongs to: its own ProjectId, else the nearest one above it.</summary>
    public async Task<string?> ProjectOfAsync(string accountId)
    {
        var all = await db.Set<Account>().AsNoTracking().Select(a => new { a.Id, a.ParentAccountId, a.ProjectId }).ToListAsync();
        var cur = all.FirstOrDefault(a => a.Id == accountId);
        var guard = 0;
        while (cur != null && guard++ < 100)
        {
            if (cur.ProjectId != null) return cur.ProjectId;
            cur = all.FirstOrDefault(a => a.Id == cur.ParentAccountId);
        }
        return null;
    }
}
