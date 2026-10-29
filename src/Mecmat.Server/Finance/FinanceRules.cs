using Microsoft.EntityFrameworkCore;
using Mecmat.Crud;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Finance;

public static class FinanceRules
{
    /// <summary>Thai government fiscal year (1 Oct – 30 Sep), stored as the Christian year it ends in;
    /// the UI adds 543 for พ.ศ.</summary>
    public static int FiscalYear(DateOnly d) => d.Month >= 10 ? d.Year + 1 : d.Year;

    public static async Task BeforeAccountSaveAsync(CrudContext c, Account x, Account? before)
    {
        x.Code = x.Code.Trim().ToUpperInvariant();
        if (x.Code.Length == 0) throw new AppException("finance.codeRequired");
        if (await c.Db.Set<Account>().AnyAsync(a => a.Code == x.Code && a.Id != x.Id)) throw new AppException("finance.codeTaken");
        if (x.ParentAccountId == null) return;
        if (x.ParentAccountId == x.Id) throw new AppException("finance.cycle");
        // Walk up from the new parent; meeting this account again would make the tree a loop.
        var all = await c.Db.Set<Account>().AsNoTracking().Select(a => new { a.Id, a.ParentAccountId }).ToListAsync();
        var cur = x.ParentAccountId;
        var guard = 0;
        while (cur != null && guard++ < 100)
        {
            if (cur == x.Id) throw new AppException("finance.cycle");
            cur = all.FirstOrDefault(a => a.Id == cur)?.ParentAccountId;
        }
        if (before?.ParentAccountId != x.ParentAccountId)
        {
            // Money lives only on leaves (ADR 0004). An account that already holds entries cannot
            // become a parent, or its own money would be double-counted with its children's.
            var parentHasMoney = await c.Db.Set<LedgerEntry>().AnyAsync(l => l.FromAccountId == x.ParentAccountId || l.ToAccountId == x.ParentAccountId);
            if (parentHasMoney) throw new AppException("finance.parentHasEntries");
        }
    }

    public static async Task BeforeAccountDeleteAsync(AppDbContext db, Account a)
    {
        if (await db.Set<LedgerEntry>().AnyAsync(l => l.FromAccountId == a.Id || l.ToAccountId == a.Id))
            throw new AppException("finance.accountHasEntries");
        if (await db.Set<Account>().AnyAsync(x => x.ParentAccountId == a.Id && x.RecordStatus != RecordStatus.Deleted))
            throw new AppException("finance.accountHasChildren");
    }
}
