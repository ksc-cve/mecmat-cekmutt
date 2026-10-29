using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/approvals")]
public sealed class ApprovalsController(AppDbContext db, CurrentUser cu, ApprovalEngine engine) : ControllerBase
{
    public sealed record DecideBody(ApprovalDecision Decision, string? Comment);

    /// <summary>What is waiting for me, including what I may decide on someone's behalf.</summary>
    [HttpGet("inbox")]
    public async Task<IActionResult> Inbox()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var pending = await db.Set<ApprovalInstance>().AsNoTracking().Where(i => i.Status == ApprovalStatus.Pending).ToListAsync();
        var mine = new List<object>();
        foreach (var i in pending)
        {
            var deciders = await engine.CurrentDecidersAsync(i);
            if (!deciders.TryGetValue(cu.PersonId!, out var onBehalf)) continue;
            mine.Add(new { i.Id, i.SubjectType, i.SubjectId, i.RequesterPersonId, i.SubmittedAt, i.CurrentStep, steps = ApprovalEngine.Steps(i).Count,
                onBehalfOf = onBehalf, summary = await SummaryAsync(i) });
        }
        var myRequests = await db.Set<ApprovalInstance>().AsNoTracking().Where(i => i.RequesterPersonId == cu.PersonId)
            .OrderByDescending(i => i.SubmittedAt).Take(50).ToListAsync();
        return Ok(new { waiting = mine, submitted = myRequests });
    }

    private async Task<object?> SummaryAsync(ApprovalInstance i) => i.SubjectType switch
    {
        nameof(MoneyRequest) => await db.Set<MoneyRequest>().AsNoTracking().Where(r => r.Id == i.SubjectId)
            .Select(r => new { r.Kind, r.Purpose, r.TotalAmount, r.AccountId, r.ProjectId }).FirstOrDefaultAsync(),
        nameof(Booking) => await db.Set<Booking>().AsNoTracking().Where(b => b.Id == i.SubjectId)
            .Select(b => new { b.Title, b.StartAt, b.EndAt, b.ResourceId, b.ProjectId }).FirstOrDefaultAsync(),
        _ => null,
    };

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var i = await db.Set<ApprovalInstance>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        var steps = ApprovalEngine.Steps(i);
        var involved = i.RequesterPersonId == cu.PersonId || steps.Any(s => s.Approvers.Contains(cu.PersonId!))
                       || cu.CanAll(Modules.ApprovalsConfig, PermAction.View);
        if (!involved) throw new AppException("errors.forbidden", 403);
        var actions = await db.Set<ApprovalAction>().AsNoTracking().Where(a => a.InstanceId == id).OrderBy(a => a.At).ToListAsync();
        var deciders = await engine.CurrentDecidersAsync(i);
        return Ok(new { instance = i, steps, actions, canDecide = deciders.ContainsKey(cu.PersonId!), summary = await SummaryAsync(i) });
    }

    [HttpPost("{id}/decide")]
    public async Task<IActionResult> Decide(string id, DecideBody b)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var inst = await engine.DecideAsync(id, cu.PersonId!, b.Decision, b.Comment?.Trim());
        return Ok(new { inst.Status, inst.CurrentStep });
    }
}
