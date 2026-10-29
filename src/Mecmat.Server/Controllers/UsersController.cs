using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/users")]
public sealed class UsersController(AppDbContext db, CurrentUser cu, SessionService sessions, NotificationService notify) : ControllerBase
{
    public sealed record ApproveBody(string[]? RoleKeys, DateOnly? ExpiresAt, string? LinkPersonId);
    public sealed record RolesBody(string[] RoleKeys);
    public sealed record CreateBody(string PersonId, string Username, string[]? RoleKeys, DateOnly? ExpiresAt);
    public sealed record ExpiryBody(DateOnly? ExpiresAt);

    private void Require(PermAction a)
    {
        if (!cu.CanAll(Modules.Users, a)) throw new AppException("errors.forbidden", 403);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        Require(PermAction.View);
        var users = await db.Set<UserAccount>().AsNoTracking().OrderBy(u => u.Username).ToListAsync();
        var people = await db.Set<Person>().AsNoTracking().ToDictionaryAsync(p => p.Id);
        var roles = await db.Set<Role>().AsNoTracking().ToDictionaryAsync(r => r.Id);
        var links = await db.Set<UserRole>().AsNoTracking().Where(r => r.RecordStatus == RecordStatus.Active).ToListAsync();
        var resets = await db.Set<PasswordResetRequest>().AsNoTracking()
            .Where(r => r.Status == ResetRequestStatus.Open).ToListAsync();
        return Ok(new
        {
            users = users.Select(u => new
            {
                u.Id, u.Username, u.Status, u.IsAdmin, u.ExpiresAt, u.LastLoginAt, u.TotpEnabled, u.MustChangePassword,
                u.CreatedAt, u.SyncStamp,
                person = people.TryGetValue(u.PersonId, out var p)
                    ? new { p.Id, p.TitleTh, p.FirstNameTh, p.LastNameTh, p.Category, p.WorkEmail } : null,
                roles = links.Where(l => l.UserId == u.Id).Select(l => roles[l.RoleId].Key).ToArray(),
                resetRequested = resets.Any(r => r.UserId == u.Id),
            }),
            resetRequests = resets.Select(r => new { r.Id, r.UserId, r.UsernameTyped, r.Note, r.CreatedAt }),
            roles = roles.Values.OrderBy(r => r.Key).Select(r => new { r.Id, r.Key, r.NameTh, r.NameEn }),
        });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id, ApproveBody b)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        if (u.Status != AccountStatus.PendingApproval) throw new AppException("users.notPending");
        // A sign-up may belong to someone already on file (e.g. an admin created their profile
        // first). Linking moves the account to that profile and drops the stub the form made.
        if (b.LinkPersonId != null && b.LinkPersonId != u.PersonId)
        {
            if (await db.Set<UserAccount>().AnyAsync(x => x.PersonId == b.LinkPersonId))
                throw new AppException("users.personHasAccount");
            var stub = await db.Set<Person>().FirstAsync(p => p.Id == u.PersonId);
            stub.RecordStatus = RecordStatus.Deleted;
            u.PersonId = b.LinkPersonId;
        }
        u.Status = AccountStatus.Active;
        u.ExpiresAt = b.ExpiresAt;
        await SetRolesAsync(u, b.RoleKeys ?? ["researcher"]);
        await notify.NotifyUsersAsync([u.Id], "account.approved", "notify.accountApproved");
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        if (u.Status != AccountStatus.PendingApproval) throw new AppException("users.notPending");
        u.Status = AccountStatus.Closed;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    /// <summary>Returns the one-time password exactly once. Only its hash is stored, so neither the
    /// admin nor anyone with the database can learn the password the person later chooses.</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        if (u.Id == cu.UserId) throw new AppException("users.cannotResetSelf");
        var otp = PasswordHasher.GenerateOneTime();
        u.PasswordHash = PasswordHasher.Hash(otp);
        u.PasswordChangedAt = DateTime.UtcNow;
        u.MustChangePassword = true;
        u.FailedLoginCount = 0;
        u.LockedUntil = null;
        await sessions.RevokeAllAsync(u.Id, "admin-reset");
        var open = await db.Set<PasswordResetRequest>().Where(r => r.UserId == u.Id && r.Status == ResetRequestStatus.Open).ToListAsync();
        foreach (var r in open) { r.Status = ResetRequestStatus.Done; r.HandledBy = cu.UserId; r.HandledAt = DateTime.UtcNow; }
        db.LogEvent(nameof(UserAccount), u.Id, "PasswordReset");
        await db.SaveChangesAsync();
        return Ok(new { oneTimePassword = otp });
    }

    [HttpPost("reset-requests/{id}/dismiss")]
    public async Task<IActionResult> DismissReset(string id)
    {
        Require(PermAction.Edit);
        var r = await db.Set<PasswordResetRequest>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        r.Status = ResetRequestStatus.Rejected; r.HandledBy = cu.UserId; r.HandledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/reset-totp")]
    public async Task<IActionResult> ResetTotp(string id)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        u.TotpEnabled = false; u.TotpSecret = null; u.RecoveryCodesHash = null;
        await sessions.RevokeAllAsync(u.Id, "totp-reset");
        db.LogEvent(nameof(UserAccount), u.Id, "TotpReset");
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> Suspend(string id)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        if (u.Id == cu.UserId) throw new AppException("users.cannotSuspendSelf");
        u.Status = AccountStatus.Suspended;
        await sessions.RevokeAllAsync(u.Id, "suspended");
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(string id)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        if (u.Status is AccountStatus.PendingApproval) throw new AppException("users.useApprove");
        u.Status = AccountStatus.Active;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> Roles(string id, RolesBody b)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        await SetRolesAsync(u, b.RoleKeys);
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPut("{id}/expiry")]
    public async Task<IActionResult> Expiry(string id, ExpiryBody b)
    {
        Require(PermAction.Edit);
        var u = await Load(id);
        u.ExpiresAt = b.ExpiresAt;
        if (u.Status == AccountStatus.Expired && (b.ExpiresAt == null || b.ExpiresAt >= Clock.TodayBangkok()))
            u.Status = AccountStatus.Active;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBody b)
    {
        Require(PermAction.Edit);
        var username = b.Username.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-z0-9._-]{3,40}$")) throw new AppException("auth.username.invalid");
        if (await db.Set<UserAccount>().AnyAsync(u => u.Username == username)) throw new AppException("auth.username.taken");
        if (await db.Set<UserAccount>().AnyAsync(u => u.PersonId == b.PersonId)) throw new AppException("users.personHasAccount");
        if (!await db.Set<Person>().AnyAsync(p => p.Id == b.PersonId)) throw new AppException("errors.notFound", 404);
        var otp = PasswordHasher.GenerateOneTime();
        var u = new UserAccount
        {
            PersonId = b.PersonId, Username = username, PasswordHash = PasswordHasher.Hash(otp),
            PasswordChangedAt = DateTime.UtcNow, MustChangePassword = true, Status = AccountStatus.Active, ExpiresAt = b.ExpiresAt,
        };
        db.Add(u);
        await db.SaveChangesAsync();
        await SetRolesAsync(u, b.RoleKeys ?? ["researcher"]);
        await db.SaveChangesAsync();
        return Ok(new { id = u.Id, oneTimePassword = otp });
    }

    private async Task<UserAccount> Load(string id) =>
        await db.Set<UserAccount>().FirstOrDefaultAsync(u => u.Id == id) ?? throw new AppException("errors.notFound", 404);

    private async Task SetRolesAsync(UserAccount u, string[] keys)
    {
        // Only an admin may hand out the admin role; a user manager cannot promote anyone above themselves.
        if (keys.Contains("admin") && !cu.IsAdmin) throw new AppException("errors.forbidden", 403);
        var roles = await db.Set<Role>().Where(r => keys.Contains(r.Key)).ToListAsync();
        if (roles.Count != keys.Distinct().Count()) throw new AppException("users.unknownRole");
        var current = await db.Set<UserRole>().Where(r => r.UserId == u.Id).ToListAsync();
        foreach (var c in current)
            c.RecordStatus = roles.Any(r => r.Id == c.RoleId) ? RecordStatus.Active : RecordStatus.Deleted;
        foreach (var r in roles.Where(r => current.All(c => c.RoleId != r.Id)))
            db.Add(new UserRole { UserId = u.Id, RoleId = r.Id });
        u.IsAdmin = keys.Contains("admin");
    }
}
