using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api/notifications")]
public sealed class NotificationsController(AppDbContext db, CurrentUser cu) : ControllerBase
{
    /// <summary>Polled by the app every 30 s; "since" keeps the reply tiny when nothing changed.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool all = false)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var q = db.Set<Notification>().AsNoTracking().Where(n => n.RecipientUserId == cu.UserId && n.RecordStatus == RecordStatus.Active);
        var unread = await q.CountAsync(n => n.ReadAt == null);
        if (!all) q = q.Where(n => n.ReadAt == null || n.ModifiedAt > DateTime.UtcNow.AddDays(-14));
        var rows = await q.OrderByDescending(n => n.ModifiedAt).Take(100).ToListAsync();
        return Ok(new { unread, rows });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> Read(string id)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var n = await db.Set<Notification>().FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == cu.UserId)
                ?? throw new AppException("errors.notFound", 404);
        n.ReadAt ??= DateTime.UtcNow;
        db.SkipAudit = true;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        var rows = await db.Set<Notification>().Where(x => x.RecipientUserId == cu.UserId && x.ReadAt == null).ToListAsync();
        foreach (var n in rows) n.ReadAt = DateTime.UtcNow;
        db.SkipAudit = true;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
