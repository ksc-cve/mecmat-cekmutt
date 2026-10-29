using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

public sealed record ApprovalRequest(
    string SubjectType, string SubjectId, string RequesterPersonId, string? RequestKind = null,
    long? Amount = null, string? AccountId = null, string? ProjectId = null, string? ResourceId = null);

public sealed class SnapshotStep
{
    public int Step { get; set; }
    public StepRule Rule { get; set; }
    public List<string> Approvers { get; set; } = [];
}

/// <summary>Called when an approval closes, so the subject (a money request, a booking …) can move
/// to its own approved/rejected state. One handler per SubjectType.</summary>
public interface IApprovalHandler
{
    string SubjectType { get; }
    Task OnClosedAsync(AppDbContext db, ApprovalInstance instance);
}

public sealed class ApprovalException(string key) : Exception(key)
{
    public string Key { get; } = key;
}

public sealed class ApprovalEngine(AppDbContext db, NotificationService notify, IEnumerable<IApprovalHandler> handlers)
{
    public async Task<ApprovalInstance> SubmitAsync(ApprovalRequest r)
    {
        var policy = await FindPolicyAsync(r) ?? throw new ApprovalException("approval.noPolicy");
        var steps = await db.Set<ApprovalPolicyStep>().AsNoTracking()
            .Where(s => s.PolicyId == policy.Id && s.RecordStatus == RecordStatus.Active)
            .OrderBy(s => s.StepOrder).ToListAsync();

        var snapshot = new List<SnapshotStep>();
        foreach (var s in steps)
        {
            var people = (await ResolveAsync(s, r)).Where(p => p != r.RequesterPersonId).Distinct().ToList();
            // A step that resolves only to the requester is dropped: nobody approves their own
            // request, and the remaining (higher) steps still apply (ADR 0005).
            if (people.Count > 0) snapshot.Add(new SnapshotStep { Step = snapshot.Count + 1, Rule = s.Rule, Approvers = people });
        }
        if (snapshot.Count == 0)
        {
            var fallback = (await GroupHeadsAsync()).Where(p => p != r.RequesterPersonId).ToList();
            if (fallback.Count == 0) fallback = await AdminPeopleAsync(r.RequesterPersonId);
            if (fallback.Count == 0) throw new ApprovalException("approval.noApprover");
            snapshot.Add(new SnapshotStep { Step = 1, Rule = StepRule.Any, Approvers = fallback });
        }

        var inst = new ApprovalInstance
        {
            SubjectType = r.SubjectType, SubjectId = r.SubjectId, PolicyId = policy.Id,
            PolicySnapshotJson = JsonSerializer.Serialize(snapshot, Json.Options),
            RequesterPersonId = r.RequesterPersonId, SubmittedAt = DateTime.UtcNow,
        };
        db.Add(inst);
        await db.SaveChangesAsync(); // assigns the instance id used in links below
        await NotifyStepAsync(inst, snapshot[0]);
        return inst;
    }

    public static List<SnapshotStep> Steps(ApprovalInstance i) =>
        JsonSerializer.Deserialize<List<SnapshotStep>>(i.PolicySnapshotJson, Json.Options) ?? [];

    /// <summary>People who may decide the current step: the named approvers plus anyone holding an
    /// active delegation from one of them.</summary>
    public async Task<Dictionary<string, string?>> CurrentDecidersAsync(ApprovalInstance i)
    {
        var step = Steps(i).FirstOrDefault(s => s.Step == i.CurrentStep);
        var map = new Dictionary<string, string?>();
        if (step == null || i.Status != ApprovalStatus.Pending) return map;
        foreach (var a in step.Approvers) map[a] = null;
        var today = Clock.TodayBangkok();
        var dels = await db.Set<Delegation>().AsNoTracking()
            .Where(d => step.Approvers.Contains(d.FromPersonId) && d.FromDate <= today && d.ToDate >= today
                        && d.RecordStatus == RecordStatus.Active && (d.SubjectType == null || d.SubjectType == i.SubjectType))
            .ToListAsync();
        foreach (var d in dels) map.TryAdd(d.ToPersonId, d.FromPersonId);
        map.Remove(i.RequesterPersonId);
        return map;
    }

    public async Task<ApprovalInstance> DecideAsync(string instanceId, string actorPersonId, ApprovalDecision decision, string? comment)
    {
        var inst = await db.Set<ApprovalInstance>().FirstOrDefaultAsync(x => x.Id == instanceId)
                   ?? throw new ApprovalException("errors.notFound");
        if (inst.Status != ApprovalStatus.Pending) throw new ApprovalException("approval.closed");
        var deciders = await CurrentDecidersAsync(inst);
        if (!deciders.TryGetValue(actorPersonId, out var onBehalfOf)) throw new ApprovalException("approval.notYourTurn");
        if (decision is ApprovalDecision.Reject or ApprovalDecision.Return && string.IsNullOrWhiteSpace(comment))
            throw new ApprovalException("approval.commentRequired");

        db.Add(new ApprovalAction
        {
            InstanceId = inst.Id, StepOrder = inst.CurrentStep, ActorPersonId = actorPersonId,
            OnBehalfOfPersonId = onBehalfOf, Decision = decision, Comment = comment, At = DateTime.UtcNow,
        });
        db.LogEvent(nameof(ApprovalInstance), inst.Id, "Decide",
            new { inst.SubjectType, inst.SubjectId, decision, inst.CurrentStep, onBehalfOf, comment });
        if (decision == ApprovalDecision.Comment) { await db.SaveChangesAsync(); return inst; }

        var steps = Steps(inst);
        var step = steps.First(s => s.Step == inst.CurrentStep);
        switch (decision)
        {
            case ApprovalDecision.Approve:
                var stepDone = step.Rule == StepRule.Any || await AllApprovedAsync(inst, step, actorPersonId, onBehalfOf);
                if (!stepDone) break;
                if (inst.CurrentStep < steps.Count)
                {
                    inst.CurrentStep++;
                    await NotifyStepAsync(inst, steps.First(s => s.Step == inst.CurrentStep));
                }
                else await CloseAsync(inst, ApprovalStatus.Approved);
                break;
            case ApprovalDecision.Reject: await CloseAsync(inst, ApprovalStatus.Rejected); break;
            case ApprovalDecision.Return: await CloseAsync(inst, ApprovalStatus.Returned); break;
        }
        await db.SaveChangesAsync();
        return inst;
    }

    public async Task CancelAsync(ApprovalInstance inst)
    {
        if (inst.Status != ApprovalStatus.Pending) return;
        inst.Status = ApprovalStatus.Cancelled;
        inst.ClosedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    private async Task<bool> AllApprovedAsync(ApprovalInstance inst, SnapshotStep step, string actor, string? onBehalfOf)
    {
        var done = await db.Set<ApprovalAction>().AsNoTracking()
            .Where(a => a.InstanceId == inst.Id && a.StepOrder == step.Step && a.Decision == ApprovalDecision.Approve)
            .Select(a => a.OnBehalfOfPersonId ?? a.ActorPersonId).ToListAsync();
        done.Add(onBehalfOf ?? actor);
        return step.Approvers.All(done.Contains);
    }

    private async Task CloseAsync(ApprovalInstance inst, ApprovalStatus status)
    {
        inst.Status = status;
        inst.ClosedAt = DateTime.UtcNow;
        var handler = handlers.FirstOrDefault(h => h.SubjectType == inst.SubjectType);
        if (handler != null) await handler.OnClosedAsync(db, inst);
        await notify.NotifyPeopleAsync([inst.RequesterPersonId], "approval.closed",
            "notify.approval." + status.ToString().ToLowerInvariant(),
            JsonSerializer.Serialize(new { subject = inst.SubjectType, id = inst.SubjectId }, Json.Options),
            Link(inst), inst.SubjectType, inst.SubjectId);
    }

    private Task NotifyStepAsync(ApprovalInstance inst, SnapshotStep step) =>
        notify.NotifyPeopleAsync(step.Approvers, "approval.pending", "notify.approval.pending",
            JsonSerializer.Serialize(new { subject = inst.SubjectType, id = inst.SubjectId }, Json.Options),
            "#/approvals", inst.SubjectType, inst.SubjectId, groupKey: "approval.pending");

    private static string Link(ApprovalInstance i) => i.SubjectType switch
    {
        nameof(MoneyRequest) => $"#/finance/requests/{i.SubjectId}",
        nameof(Booking) => $"#/bookings?id={i.SubjectId}",
        _ => "#/approvals",
    };

    private async Task<ApprovalPolicy?> FindPolicyAsync(ApprovalRequest r)
    {
        var candidates = await db.Set<ApprovalPolicy>().AsNoTracking()
            .Where(p => p.IsActive && p.RecordStatus == RecordStatus.Active && p.SubjectType == r.SubjectType)
            .ToListAsync();
        var accountChain = r.AccountId == null ? [] : await AncestorsAsync(r.AccountId);
        return candidates
            .Where(p => p.RequestKind == null || p.RequestKind == r.RequestKind)
            .Where(p => p.MinAmount == null || (r.Amount ?? 0) >= p.MinAmount)
            .Where(p => p.MaxAmount == null || (r.Amount ?? 0) <= p.MaxAmount)
            .Where(p => p.AccountScopeId == null || accountChain.Contains(p.AccountScopeId))
            .Where(p => p.ProjectScopeId == null || p.ProjectScopeId == r.ProjectId)
            // The most specific policy wins; Priority breaks ties the admin cares about.
            .OrderByDescending(p => (p.AccountScopeId != null ? 2 : 0) + (p.ProjectScopeId != null ? 2 : 0) + (p.RequestKind != null ? 1 : 0))
            .ThenBy(p => p.Priority)
            .FirstOrDefault();
    }

    private async Task<List<string>> ResolveAsync(ApprovalPolicyStep s, ApprovalRequest r)
    {
        var today = Clock.TodayBangkok();
        switch (s.ApproverKind)
        {
            case ApproverKind.Person:
                return s.ApproverRef == null ? [] : [s.ApproverRef];
            case ApproverKind.Position:
                return await db.Set<PositionAssignment>().AsNoTracking()
                    .Where(a => a.PositionId == s.ApproverRef && a.RecordStatus == RecordStatus.Active
                                && (a.FromDate == null || a.FromDate <= today) && (a.ToDate == null || a.ToDate >= today))
                    .Select(a => a.PersonId).ToListAsync();
            case ApproverKind.ProjectRole:
                // A request outside any project has no PI; the default routes then fall to the
                // account's custodian, which is what ADR 0005 prescribes for non-project money.
                if (r.ProjectId == null)
                    return await ResolveAsync(new ApprovalPolicyStep { ApproverKind = ApproverKind.AccountCustodian }, r);
                if (!Enum.TryParse<ProjectRole>(s.ApproverRef, out var role)) return [];
                return await db.Set<ProjectMember>().AsNoTracking()
                    .Where(m => m.ProjectId == r.ProjectId && m.Role == role && m.RecordStatus == RecordStatus.Active)
                    .Select(m => m.PersonId).ToListAsync();
            case ApproverKind.AccountCustodian:
                if (r.AccountId == null) return [];
                var chain = await AncestorsAsync(r.AccountId);
                var accounts = await db.Set<Account>().AsNoTracking().Where(a => chain.Contains(a.Id)).ToListAsync();
                // Nearest custodian up the tree: a sub-account without its own custodian is looked
                // after by whoever holds the account above it.
                foreach (var id in chain)
                {
                    var c = accounts.First(a => a.Id == id).CustodianPersonId;
                    if (c != null) return [c];
                }
                return [];
            case ApproverKind.RequesterAdvisor:
                var adv = await db.Set<Person>().AsNoTracking().Where(p => p.Id == r.RequesterPersonId)
                    .Select(p => p.AdvisorPersonId).FirstOrDefaultAsync();
                return adv == null ? [] : [adv];
            case ApproverKind.ResourceManager:
                if (r.ResourceId == null) return [];
                var mgr = await db.Set<Resource>().AsNoTracking().Where(x => x.Id == r.ResourceId)
                    .Select(x => x.ManagerPersonId).FirstOrDefaultAsync();
                return mgr == null ? [] : [mgr];
            default:
                return [];
        }
    }

    /// <summary>The account itself first, then each parent up to the root.</summary>
    public async Task<List<string>> AncestorsAsync(string accountId)
    {
        var all = await db.Set<Account>().AsNoTracking().Select(a => new { a.Id, a.ParentAccountId }).ToListAsync();
        var chain = new List<string>();
        string? cur = accountId;
        while (cur != null && !chain.Contains(cur))
        {
            chain.Add(cur);
            cur = all.FirstOrDefault(a => a.Id == cur)?.ParentAccountId;
        }
        return chain;
    }

    private async Task<List<string>> GroupHeadsAsync()
    {
        var today = Clock.TodayBangkok();
        var top = await db.Set<GroupPosition>().AsNoTracking().Where(p => p.RecordStatus == RecordStatus.Active)
            .OrderBy(p => p.Rank).Select(p => p.Id).FirstOrDefaultAsync();
        if (top == null) return [];
        return await db.Set<PositionAssignment>().AsNoTracking()
            .Where(a => a.PositionId == top && (a.FromDate == null || a.FromDate <= today) && (a.ToDate == null || a.ToDate >= today))
            .Select(a => a.PersonId).ToListAsync();
    }

    private Task<List<string>> AdminPeopleAsync(string except) =>
        db.Set<UserAccount>().AsNoTracking()
            .Where(u => u.IsAdmin && u.Status == AccountStatus.Active && u.PersonId != except)
            .Select(u => u.PersonId).ToListAsync();
}
