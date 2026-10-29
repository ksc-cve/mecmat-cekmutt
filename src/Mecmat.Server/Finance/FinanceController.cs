using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Finance;

[ApiController, Route("api/finance")]
public sealed class FinanceController(
    AppDbContext db, CurrentUser cu, FinanceService fin, ApprovalEngine approvals, NotificationService notify) : ControllerBase
{
    public sealed record LineBody(string Description, decimal Qty, long UnitPrice, BudgetCategory? BudgetCategory);
    public sealed record RequestBody(
        MoneyRequestKind Kind, string AccountId, string? ToAccountId, string Purpose, string? PayeePersonId,
        string? PayeeOrganizationId, string? PayeeText, DateOnly? NeededBy, DateOnly? AdvanceDueDate,
        string? RelatedAdvanceId, List<LineBody> Lines, long? SyncStamp);
    public sealed record SettleBody(DateOnly? EntryDate, string? ReferenceText);
    public sealed record IncomeBody(string AccountId, long Amount, DateOnly? EntryDate, string Description,
        string? CounterpartyOrganizationId, string? CounterpartyText, string? ReferenceText);
    public sealed record ReverseBody(string Reason);
    public sealed record AdjustBody(string AccountId, long Amount, string Description);

    private async Task<Scope> ScopeAsync()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        return HttpContext.Items["scope"] as Scope ?? (Scope)(HttpContext.Items["scope"] = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!));
    }

    [HttpGet("tree")]
    public async Task<IActionResult> Tree()
    {
        var s = await ScopeAsync();
        if (!cu.CanAny(Modules.Finance, PermAction.View) && !cu.CanAny(Modules.Finance, PermAction.Create))
            throw new AppException("errors.forbidden", 403);
        return Ok(new
        {
            fiscalYear = FinanceRules.FiscalYear(Clock.TodayBangkok()),
            accounts = await fin.TreeAsync(s),
            canCreateRoot = cu.CanAll(Modules.Finance, PermAction.Create),
            canRecordIncome = cu.CanAny(Modules.Finance, PermAction.Edit),
        });
    }

    [HttpGet("accounts/{id}/statement")]
    public async Task<IActionResult> Statement(string id)
    {
        var s = await ScopeAsync();
        var requestable = await fin.RequestableAsync(s);
        if (!cu.Can(Modules.Finance, PermAction.View, s.AccountIds.Contains(id)) && !requestable.Contains(id))
            throw new AppException("errors.forbidden", 403);
        var subtree = await SubtreeAsync(id);
        var rows = await db.Set<LedgerEntry>().AsNoTracking()
            .Where(l => (l.FromAccountId != null && subtree.Contains(l.FromAccountId)) || (l.ToAccountId != null && subtree.Contains(l.ToAccountId)))
            .OrderBy(l => l.EntryDate).ThenBy(l => l.CreatedAt).ToListAsync();
        long running = 0;
        var lines = rows.Select(l =>
        {
            var inflow = l.ToAccountId != null && subtree.Contains(l.ToAccountId);
            var outflow = l.FromAccountId != null && subtree.Contains(l.FromAccountId);
            // A transfer between two sub-accounts of this subtree moves nothing at this level.
            var delta = (inflow ? l.Amount : 0) - (outflow ? l.Amount : 0);
            running += delta;
            return new { l.Id, l.EntryDate, l.Kind, l.Description, l.FromAccountId, l.ToAccountId, l.Amount, delta, balance = running,
                l.MoneyRequestId, l.ReversesEntryId, l.ReferenceText, l.CounterpartyText, l.CounterpartyPersonId, l.CounterpartyOrganizationId };
        }).ToList();
        var reversed = rows.Where(r => r.ReversesEntryId != null).Select(r => r.ReversesEntryId!).ToHashSet();
        return Ok(new { lines, reversed, canReverse = cu.CanAll(Modules.Finance, PermAction.Edit) });
    }

    private async Task<HashSet<string>> SubtreeAsync(string id)
    {
        var all = await db.Set<Account>().AsNoTracking().Select(a => new { a.Id, a.ParentAccountId }).ToListAsync();
        var set = new HashSet<string>();
        var stack = new Stack<string>([id]);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!set.Add(cur)) continue;
            foreach (var c in all.Where(a => a.ParentAccountId == cur)) stack.Push(c.Id);
        }
        return set;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] string? status, [FromQuery] string? accountId, [FromQuery] bool mine = false)
    {
        var s = await ScopeAsync();
        IQueryable<MoneyRequest> q = db.Set<MoneyRequest>().AsNoTracking().Where(r => r.RecordStatus == RecordStatus.Active);
        if (mine || !cu.CanAll(Modules.Finance, PermAction.View))
        {
            var me = cu.PersonId;
            var accounts = s.AccountIds;
            var approving = s.ApprovalSubjectIds;
            q = mine ? q.Where(r => r.RequesterPersonId == me)
                     : q.Where(r => r.RequesterPersonId == me || accounts.Contains(r.AccountId) || approving.Contains(r.Id));
        }
        if (Enum.TryParse<MoneyRequestStatus>(status, out var st)) q = q.Where(r => r.Status == st);
        if (accountId != null) q = q.Where(r => r.AccountId == accountId);
        var rows = await q.OrderByDescending(r => r.CreatedAt).Take(500).ToListAsync();
        return Ok(new { rows, canCreate = (await fin.RequestableAsync(s)).Count > 0 });
    }

    [HttpGet("requests/{id}")]
    public async Task<IActionResult> GetRequest(string id)
    {
        var s = await ScopeAsync();
        var r = await db.Set<MoneyRequest>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        await RequireViewAsync(r, s);
        var lines = await db.Set<MoneyRequestLine>().AsNoTracking().Where(l => l.RequestId == id && l.RecordStatus == RecordStatus.Active).ToListAsync();
        ApprovalInstance? inst = r.ApprovalInstanceId == null ? null
            : await db.Set<ApprovalInstance>().AsNoTracking().FirstOrDefaultAsync(i => i.Id == r.ApprovalInstanceId);
        var actions = inst == null ? [] : await db.Set<ApprovalAction>().AsNoTracking().Where(a => a.InstanceId == inst.Id).OrderBy(a => a.At).ToListAsync();
        var deciders = inst == null ? [] : await approvals.CurrentDecidersAsync(inst);
        var mineReq = r.RequesterPersonId == cu.PersonId;
        var settleRight = cu.Can(Modules.Finance, PermAction.Edit, s.AccountIds.Contains(r.AccountId));
        return Ok(new
        {
            request = r, lines,
            approval = inst == null ? null : new { inst.Id, inst.Status, inst.CurrentStep, steps = ApprovalEngine.Steps(inst), actions },
            available = await fin.AvailableAsync(r.AccountId, r.Id),
            can = new
            {
                edit = mineReq && r.Status is MoneyRequestStatus.Draft or MoneyRequestStatus.Returned,
                submit = mineReq && r.Status is MoneyRequestStatus.Draft or MoneyRequestStatus.Returned,
                cancel = mineReq && r.Status is MoneyRequestStatus.Draft or MoneyRequestStatus.Returned or MoneyRequestStatus.Submitted or MoneyRequestStatus.InApproval,
                decide = cu.PersonId != null && deciders.ContainsKey(cu.PersonId),
                settle = settleRight && r.Status == MoneyRequestStatus.Approved,
            },
        });
    }

    private async Task RequireViewAsync(MoneyRequest r, Scope s)
    {
        if (r.RequesterPersonId == cu.PersonId || s.ApprovalSubjectIds.Contains(r.Id)) return;
        if (cu.Can(Modules.Finance, PermAction.View, s.AccountIds.Contains(r.AccountId))) return;
        // Past approvers keep sight of what they signed.
        if (r.ApprovalInstanceId != null && await db.Set<ApprovalAction>().AnyAsync(a => a.InstanceId == r.ApprovalInstanceId && a.ActorPersonId == cu.PersonId)) return;
        throw new AppException("errors.forbidden", 403);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> Create(RequestBody b)
    {
        var s = await ScopeAsync();
        var r = new MoneyRequest { RequesterPersonId = cu.PersonId! };
        await ApplyAsync(r, b, s);
        db.Add(r);
        await db.SaveChangesAsync();
        await ReplaceLinesAsync(r, b.Lines);
        await db.SaveChangesAsync();
        return Ok(new { id = r.Id });
    }

    [HttpPut("requests/{id}")]
    public async Task<IActionResult> Update(string id, RequestBody b)
    {
        var s = await ScopeAsync();
        var r = await LoadOwnAsync(id);
        if (r.Status is not (MoneyRequestStatus.Draft or MoneyRequestStatus.Returned)) throw new AppException("finance.notEditable");
        if (b.SyncStamp != null) db.Entry(r).Property(nameof(Entity.SyncStamp)).OriginalValue = b.SyncStamp.Value;
        await ApplyAsync(r, b, s);
        await ReplaceLinesAsync(r, b.Lines);
        await db.SaveChangesAsync();
        return Ok(new { id = r.Id });
    }

    private async Task ApplyAsync(MoneyRequest r, RequestBody b, Scope s)
    {
        var requestable = await fin.RequestableAsync(s);
        if (!requestable.Contains(b.AccountId)) throw new AppException("errors.forbidden", 403);
        if (!await fin.IsLeafAsync(b.AccountId)) throw new AppException("finance.notLeaf");
        if (string.IsNullOrWhiteSpace(b.Purpose)) throw new AppException("finance.purposeRequired");
        if (b.Lines.Count == 0) throw new AppException("finance.linesRequired");
        if (b.Kind == MoneyRequestKind.Transfer)
        {
            if (b.ToAccountId == null || b.ToAccountId == b.AccountId) throw new AppException("finance.transferTarget");
            if (!await fin.IsLeafAsync(b.ToAccountId)) throw new AppException("finance.notLeaf");
        }
        if (b.Kind == MoneyRequestKind.AdvanceClearance)
        {
            var adv = await db.Set<MoneyRequest>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == b.RelatedAdvanceId)
                      ?? throw new AppException("finance.advanceRequired");
            if (adv.Kind != MoneyRequestKind.Advance || adv.Status != MoneyRequestStatus.Settled || adv.RequesterPersonId != cu.PersonId)
                throw new AppException("finance.advanceInvalid");
            if (await db.Set<MoneyRequest>().AnyAsync(x => x.RelatedAdvanceId == adv.Id && x.Id != r.Id
                                                           && x.Status != MoneyRequestStatus.Rejected && x.Status != MoneyRequestStatus.Cancelled))
                throw new AppException("finance.advanceAlreadyCleared");
            b = b with { AccountId = adv.AccountId };
        }
        r.Kind = b.Kind; r.AccountId = b.AccountId; r.ToAccountId = b.Kind == MoneyRequestKind.Transfer ? b.ToAccountId : null;
        r.Purpose = b.Purpose.Trim(); r.PayeePersonId = b.PayeePersonId; r.PayeeOrganizationId = b.PayeeOrganizationId;
        r.PayeeText = b.PayeeText?.Trim(); r.NeededBy = b.NeededBy;
        r.RelatedAdvanceId = b.Kind == MoneyRequestKind.AdvanceClearance ? b.RelatedAdvanceId : null;
        var days = int.TryParse((await db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(x => x.Key == "finance.advanceDays"))?.Value, out var d) ? d : 30;
        r.AdvanceDueDate = b.Kind == MoneyRequestKind.Advance ? b.AdvanceDueDate ?? Clock.TodayBangkok().AddDays(days) : null;
        r.ProjectId = await fin.ProjectOfAsync(b.AccountId);
        r.TotalAmount = b.Lines.Sum(LineAmount);
        if (r.TotalAmount <= 0 && b.Kind != MoneyRequestKind.AdvanceClearance) throw new AppException("finance.amountPositive");
    }

    private static long LineAmount(LineBody l) =>
        l.Qty <= 0 || l.UnitPrice < 0 ? throw new AppException("finance.lineInvalid") : (long)Math.Round(l.Qty * l.UnitPrice, MidpointRounding.AwayFromZero);

    private async Task ReplaceLinesAsync(MoneyRequest r, List<LineBody> lines)
    {
        var old = await db.Set<MoneyRequestLine>().Where(l => l.RequestId == r.Id && l.RecordStatus == RecordStatus.Active).ToListAsync();
        foreach (var o in old) o.RecordStatus = RecordStatus.Deleted;
        foreach (var l in lines)
            db.Add(new MoneyRequestLine { RequestId = r.Id, Description = l.Description.Trim(), Qty = l.Qty, UnitPrice = l.UnitPrice, Amount = LineAmount(l), BudgetCategory = l.BudgetCategory });
    }

    private async Task<MoneyRequest> LoadOwnAsync(string id)
    {
        var r = await db.Set<MoneyRequest>().FirstOrDefaultAsync(x => x.Id == id && x.RecordStatus == RecordStatus.Active)
                ?? throw new AppException("errors.notFound", 404);
        if (r.RequesterPersonId != cu.PersonId) throw new AppException("errors.forbidden", 403);
        return r;
    }

    [HttpPost("requests/{id}/submit")]
    public async Task<IActionResult> Submit(string id)
    {
        await ScopeAsync();
        var r = await LoadOwnAsync(id);
        if (r.Status is not (MoneyRequestStatus.Draft or MoneyRequestStatus.Returned)) throw new AppException("finance.notEditable");
        // Checked under the write lock with the balance read inside it, so two requests submitted
        // at the same moment cannot both spend the last baht.
        await db.ExclusiveAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var draw = await DrawAsync(r);
            if (draw > 0 && !await AllowNegativeAsync(r.AccountId) && await fin.AvailableAsync(r.AccountId, r.Id) < draw)
                throw new AppException("finance.insufficient", 400, new { available = await fin.AvailableAsync(r.AccountId, r.Id) });
            // Status changes only after a route exists: a request with no matching policy stays a
            // draft instead of sitting "in approval" with nobody to approve it.
            var inst = await approvals.SubmitAsync(new ApprovalRequest(nameof(MoneyRequest), r.Id, r.RequesterPersonId, r.Kind.ToString(),
                r.TotalAmount, r.AccountId, r.ProjectId));
            r.Status = MoneyRequestStatus.InApproval;
            r.ApprovalInstanceId = inst.Id;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        return Ok(new { ok = true });
    }

    private async Task<long> DrawAsync(MoneyRequest r)
    {
        if (FinanceService.DrawsMoney(r.Kind)) return r.TotalAmount;
        var adv = await db.Set<MoneyRequest>().AsNoTracking().FirstAsync(x => x.Id == r.RelatedAdvanceId);
        return Math.Max(0, r.TotalAmount - adv.TotalAmount);
    }

    private async Task<bool> AllowNegativeAsync(string accountId) =>
        await db.Set<Account>().Where(a => a.Id == accountId).Select(a => a.AllowNegative).FirstAsync();

    [HttpPost("requests/{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        await ScopeAsync();
        var r = await LoadOwnAsync(id);
        if (r.Status is MoneyRequestStatus.Settled or MoneyRequestStatus.Rejected or MoneyRequestStatus.Cancelled or MoneyRequestStatus.Approved)
            throw new AppException("finance.notCancellable");
        if (r.ApprovalInstanceId != null)
        {
            var inst = await db.Set<ApprovalInstance>().FirstAsync(i => i.Id == r.ApprovalInstanceId);
            await approvals.CancelAsync(inst);
        }
        r.Status = MoneyRequestStatus.Cancelled;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    /// <summary>Books an approved request into the ledger. This is the only place a request turns
    /// into money movement (ADR 0004).</summary>
    [HttpPost("requests/{id}/settle")]
    public async Task<IActionResult> Settle(string id, SettleBody b)
    {
        var s = await ScopeAsync();
        await db.ExclusiveAsync(async () =>
        {
            var r = await db.Set<MoneyRequest>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
            if (!cu.Can(Modules.Finance, PermAction.Edit, s.AccountIds.Contains(r.AccountId))) throw new AppException("errors.forbidden", 403);
            if (r.Status != MoneyRequestStatus.Approved) throw new AppException("finance.notApproved");
            var date = b.EntryDate ?? Clock.TodayBangkok();
            var payee = new { r.PayeePersonId, r.PayeeOrganizationId, r.PayeeText };
            LedgerEntry Entry(LedgerKind k, string? from, string? to, long amount, string desc) => new()
            {
                EntryDate = date, Kind = k, FromAccountId = from, ToAccountId = to, Amount = amount, Description = desc,
                MoneyRequestId = r.Id, ReferenceText = b.ReferenceText, CounterpartyPersonId = payee.PayeePersonId ?? r.RequesterPersonId,
                CounterpartyOrganizationId = payee.PayeeOrganizationId, CounterpartyText = payee.PayeeText,
            };
            switch (r.Kind)
            {
                case MoneyRequestKind.Reimbursement:
                case MoneyRequestKind.Payment:
                    db.Add(Entry(LedgerKind.Expense, r.AccountId, null, r.TotalAmount, r.Purpose));
                    break;
                case MoneyRequestKind.Advance:
                    db.Add(Entry(LedgerKind.AdvanceOut, r.AccountId, null, r.TotalAmount, r.Purpose));
                    break;
                case MoneyRequestKind.Transfer:
                    db.Add(Entry(LedgerKind.Transfer, r.AccountId, r.ToAccountId, r.TotalAmount, r.Purpose));
                    break;
                case MoneyRequestKind.AdvanceClearance:
                    var adv = await db.Set<MoneyRequest>().FirstAsync(x => x.Id == r.RelatedAdvanceId);
                    var diff = adv.TotalAmount - r.TotalAmount;
                    // Unspent cash comes back to the account; overspend is booked as an extra expense.
                    if (diff > 0) db.Add(Entry(LedgerKind.AdvanceReturn, null, r.AccountId, diff, "คืนเงินยืม " + adv.Id));
                    if (diff < 0) db.Add(Entry(LedgerKind.Expense, r.AccountId, null, -diff, "จ่ายเกินเงินยืม " + adv.Id));
                    break;
            }
            r.Status = MoneyRequestStatus.Settled;
            r.SettledAt = DateTime.UtcNow;
            r.SettledBy = cu.UserId;
            await notify.NotifyPeopleAsync([r.RequesterPersonId], "finance.settled", "notify.finance.settled",
                System.Text.Json.JsonSerializer.Serialize(new { id = r.Id }, Json.Options), $"#/finance/requests/{r.Id}", nameof(MoneyRequest), r.Id);
            await db.SaveChangesAsync();
        });
        return Ok(new { ok = true });
    }

    [HttpPost("income")]
    public async Task<IActionResult> Income(IncomeBody b)
    {
        var s = await ScopeAsync();
        if (!cu.Can(Modules.Finance, PermAction.Edit, s.AccountIds.Contains(b.AccountId))) throw new AppException("errors.forbidden", 403);
        if (b.Amount <= 0) throw new AppException("finance.amountPositive");
        if (!await fin.IsLeafAsync(b.AccountId)) throw new AppException("finance.notLeaf");
        db.Add(new LedgerEntry
        {
            EntryDate = b.EntryDate ?? Clock.TodayBangkok(), Kind = LedgerKind.Income, ToAccountId = b.AccountId, Amount = b.Amount,
            Description = b.Description.Trim(), CounterpartyOrganizationId = b.CounterpartyOrganizationId,
            CounterpartyText = b.CounterpartyText, ReferenceText = b.ReferenceText,
        });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustBody b)
    {
        await ScopeAsync();
        if (!cu.CanAll(Modules.Finance, PermAction.Manage)) throw new AppException("errors.forbidden", 403);
        if (b.Amount == 0 || string.IsNullOrWhiteSpace(b.Description)) throw new AppException("finance.adjustInvalid");
        if (!await fin.IsLeafAsync(b.AccountId)) throw new AppException("finance.notLeaf");
        db.Add(new LedgerEntry
        {
            EntryDate = Clock.TodayBangkok(), Kind = LedgerKind.Adjustment, Amount = Math.Abs(b.Amount), Description = b.Description.Trim(),
            FromAccountId = b.Amount < 0 ? b.AccountId : null, ToAccountId = b.Amount > 0 ? b.AccountId : null,
        });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    /// <summary>Corrections never edit an entry: a mirror entry cancels it and the history keeps both.</summary>
    [HttpPost("ledger/{id}/reverse")]
    public async Task<IActionResult> Reverse(string id, ReverseBody b)
    {
        await ScopeAsync();
        if (!cu.CanAll(Modules.Finance, PermAction.Edit)) throw new AppException("errors.forbidden", 403);
        if (string.IsNullOrWhiteSpace(b.Reason)) throw new AppException("finance.reasonRequired");
        var e = await db.Set<LedgerEntry>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        if (e.Kind == LedgerKind.Reversal) throw new AppException("finance.cannotReverseReversal");
        if (await db.Set<LedgerEntry>().AnyAsync(x => x.ReversesEntryId == id)) throw new AppException("finance.alreadyReversed");
        db.Add(new LedgerEntry
        {
            EntryDate = Clock.TodayBangkok(), Kind = LedgerKind.Reversal, FromAccountId = e.ToAccountId, ToAccountId = e.FromAccountId,
            Amount = e.Amount, Description = "กลับรายการ: " + b.Reason.Trim(), ReversesEntryId = e.Id, MoneyRequestId = e.MoneyRequestId,
        });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpGet("advances")]
    public async Task<IActionResult> Advances()
    {
        var s = await ScopeAsync();
        var settled = await db.Set<MoneyRequest>().AsNoTracking()
            .Where(r => r.Kind == MoneyRequestKind.Advance && r.Status == MoneyRequestStatus.Settled).ToListAsync();
        var cleared = await db.Set<MoneyRequest>().AsNoTracking()
            .Where(r => r.Kind == MoneyRequestKind.AdvanceClearance && r.Status == MoneyRequestStatus.Settled)
            .Select(r => r.RelatedAdvanceId).ToListAsync();
        var all = cu.CanAll(Modules.Finance, PermAction.View);
        var rows = settled.Where(a => !cleared.Contains(a.Id))
            .Where(a => all || a.RequesterPersonId == cu.PersonId || s.AccountIds.Contains(a.AccountId))
            .OrderBy(a => a.AdvanceDueDate)
            .Select(a => new { a.Id, a.RequesterPersonId, a.AccountId, a.Purpose, outstanding = a.TotalAmount, a.AdvanceDueDate,
                overdue = a.AdvanceDueDate < Clock.TodayBangkok() });
        return Ok(rows);
    }

    [HttpGet("budget")]
    public async Task<IActionResult> Budget([FromQuery] int? fy)
    {
        var s = await ScopeAsync();
        var year = fy ?? FinanceRules.FiscalYear(Clock.TodayBangkok());
        var (from, to) = (new DateOnly(year - 1, 10, 1), new DateOnly(year, 9, 30));
        var visible = (await fin.TreeAsync(s)).Select(n => n.Id).ToHashSet();
        var budgets = await db.Set<AccountBudget>().AsNoTracking().Where(b => b.FiscalYear == year && b.RecordStatus == RecordStatus.Active).ToListAsync();
        var entries = await db.Set<LedgerEntry>().AsNoTracking().Where(l => l.EntryDate >= from && l.EntryDate <= to).ToListAsync();
        var accounts = await db.Set<Account>().AsNoTracking().Where(a => visible.Contains(a.Id)).ToListAsync();
        return Ok(new
        {
            fiscalYear = year,
            rows = accounts.OrderBy(a => a.Code).Select(a => new
            {
                a.Id, a.Code, a.NameTh, a.NameEn, a.ParentAccountId, a.BudgetCategory,
                budget = budgets.Where(b => b.AccountId == a.Id).Sum(b => b.Amount),
                income = entries.Where(e => e.ToAccountId == a.Id && e.FromAccountId == null && e.Kind is LedgerKind.Income or LedgerKind.Adjustment).Sum(e => e.Amount),
                spent = entries.Where(e => e.FromAccountId == a.Id && e.ToAccountId == null).Sum(e => e.Amount)
                        - entries.Where(e => e.ToAccountId == a.Id && e.FromAccountId == null && e.Kind is LedgerKind.AdvanceReturn or LedgerKind.Reversal).Sum(e => e.Amount),
            }),
        });
    }
}

/// <summary>Moves a money request along when its approval closes.</summary>
public sealed class MoneyRequestApprovalHandler(NotificationService notify) : IApprovalHandler
{
    public string SubjectType => nameof(MoneyRequest);

    public async Task OnClosedAsync(AppDbContext db, ApprovalInstance inst)
    {
        var r = await db.Set<MoneyRequest>().FirstAsync(x => x.Id == inst.SubjectId);
        r.Status = inst.Status switch
        {
            ApprovalStatus.Approved => MoneyRequestStatus.Approved,
            ApprovalStatus.Rejected => MoneyRequestStatus.Rejected,
            ApprovalStatus.Returned => MoneyRequestStatus.Returned,
            _ => MoneyRequestStatus.Cancelled,
        };
        if (r.Status != MoneyRequestStatus.Approved) return;
        // Whoever books money for this account should know there is something to settle.
        var roleIds = await db.Set<RolePermission>().AsNoTracking()
            .Where(p => p.Module == Modules.Finance && p.AllAction >= PermAction.Edit).Select(p => p.RoleId).ToListAsync();
        var users = await db.Set<UserRole>().AsNoTracking().Where(u => roleIds.Contains(u.RoleId) && u.RecordStatus == RecordStatus.Active)
            .Select(u => u.UserId).ToListAsync();
        await notify.NotifyUsersAsync(users, "finance.toSettle", "notify.finance.toSettle",
            System.Text.Json.JsonSerializer.Serialize(new { id = r.Id }, Json.Options), $"#/finance/requests/{r.Id}",
            nameof(MoneyRequest), r.Id, groupKey: "finance.toSettle");
    }
}
