using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Auth;

public sealed class AccessService(AppDbContext db)
{
    public async Task LoadGrantsAsync(CurrentUser cu, UserAccount user)
    {
        cu.Grants.Clear();
        var roleIds = await db.Set<UserRole>().AsNoTracking()
            .Where(r => r.UserId == user.Id && r.RecordStatus == RecordStatus.Active).Select(r => r.RoleId).ToListAsync();
        var perms = await db.Set<RolePermission>().AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId) && p.RecordStatus == RecordStatus.Active).ToListAsync();
        foreach (var p in perms)
        {
            var g = new Grant(p.RelatedAction, p.AllAction);
            cu.Grants[p.Module] = cu.Grants.TryGetValue(p.Module, out var cur) ? Grant.Max(cur, g) : g;
        }
        var overrides = await db.Set<UserPermissionOverride>().AsNoTracking()
            .Where(o => o.UserId == user.Id && o.RecordStatus == RecordStatus.Active).ToListAsync();
        foreach (var o in overrides) cu.Grants[o.Module] = new Grant(o.RelatedAction, o.AllAction);
    }

    /// <summary>True when the person sits on any money approval route — as a named approver or by
    /// holding a position a money policy names — which makes TOTP mandatory for them.</summary>
    public async Task<bool> IsMoneyApproverAsync(string personId)
    {
        var steps = await db.Set<ApprovalPolicyStep>().AsNoTracking()
            .Join(db.Set<ApprovalPolicy>().Where(p => p.IsActive && p.SubjectType == nameof(MoneyRequest)),
                s => s.PolicyId, p => p.Id, (s, p) => s)
            .ToListAsync();
        if (steps.Any(s => s.ApproverKind == ApproverKind.Person && s.ApproverRef == personId)) return true;
        var positionIds = steps.Where(s => s.ApproverKind == ApproverKind.Position).Select(s => s.ApproverRef).ToHashSet();
        var today = Services.Clock.TodayBangkok();
        return await db.Set<PositionAssignment>().AsNoTracking().AnyAsync(a =>
            a.PersonId == personId && positionIds.Contains(a.PositionId) && a.RecordStatus == RecordStatus.Active
            && (a.FromDate == null || a.FromDate <= today) && (a.ToDate == null || a.ToDate >= today));
    }
}
