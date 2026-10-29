using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

/// <summary>In-app notifications only — the owner chose no email (ADR 0003). Rows are added to the
/// caller's DbContext, so a notification exists exactly when the change that caused it commits.</summary>
public sealed class NotificationService(AppDbContext db)
{
    public async Task NotifyUsersAsync(IEnumerable<string> userIds, string kind, string title, string? body = null,
        string? link = null, string? subjectType = null, string? subjectId = null, string? groupKey = null)
    {
        foreach (var uid in userIds.Distinct())
        {
            if (groupKey != null)
            {
                // Fold repeats into the unread row the person already has, instead of a pile of rows.
                var existing = db.Set<Notification>().Local.FirstOrDefault(n => n.RecipientUserId == uid && n.GroupKey == groupKey && n.ReadAt == null)
                    ?? await db.Set<Notification>().FirstOrDefaultAsync(n => n.RecipientUserId == uid && n.GroupKey == groupKey && n.ReadAt == null);
                if (existing != null)
                {
                    existing.Count++;
                    existing.Title = title; existing.Body = body; existing.Link = link ?? existing.Link;
                    continue;
                }
            }
            db.Add(new Notification
            {
                RecipientUserId = uid, Kind = kind, Title = title, Body = body, Link = link,
                SubjectType = subjectType, SubjectId = subjectId, GroupKey = groupKey,
            });
        }
    }

    public async Task NotifyPeopleAsync(IEnumerable<string> personIds, string kind, string title, string? body = null,
        string? link = null, string? subjectType = null, string? subjectId = null, string? groupKey = null)
    {
        var ids = personIds.Distinct().ToList();
        var users = await db.Set<UserAccount>().AsNoTracking()
            .Where(u => ids.Contains(u.PersonId) && u.Status == AccountStatus.Active).Select(u => u.Id).ToListAsync();
        await NotifyUsersAsync(users, kind, title, body, link, subjectType, subjectId, groupKey);
    }

    /// <summary>Admins plus anyone granted user management — the people who act on sign-ups and
    /// password-reset requests.</summary>
    public async Task NotifyUserManagersAsync(string kind, string title, string? body, string? link, string? groupKey)
    {
        var admins = await db.Set<UserAccount>().AsNoTracking()
            .Where(u => u.IsAdmin && u.Status == AccountStatus.Active).Select(u => u.Id).ToListAsync();
        var roleIds = await db.Set<RolePermission>().AsNoTracking()
            .Where(p => p.Module == Auth.Modules.Users && p.AllAction >= PermAction.Edit).Select(p => p.RoleId).ToListAsync();
        var managers = await db.Set<UserRole>().AsNoTracking().Where(r => roleIds.Contains(r.RoleId)).Select(r => r.UserId).ToListAsync();
        await NotifyUsersAsync(admins.Concat(managers), kind, title, body, link, null, null, groupKey);
    }
}
