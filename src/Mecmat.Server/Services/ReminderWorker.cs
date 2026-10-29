using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

/// <summary>Time-based reminders and account expiry, every 15 minutes. Each reminder is keyed in
/// ReminderSent so a restart or a slow tick never sends it twice.</summary>
public sealed class ReminderWorker(IServiceScopeFactory scopes, ILogger<ReminderWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stop);
        while (!stop.IsCancellationRequested)
        {
            try { await TickAsync(stop); }
            catch (Exception e) when (e is not OperationCanceledException) { log.LogError(e, "Reminder tick failed"); }
            await Task.Delay(TimeSpan.FromMinutes(15), stop);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notify = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var today = Clock.TodayBangkok();
        var now = DateTime.UtcNow;
        var sent = (await db.RemindersSent.AsNoTracking().Select(r => r.Key).ToListAsync(ct)).ToHashSet();

        async Task Once(string key, IEnumerable<string> personIds, string title, object args, string link, string? subjectType = null, string? subjectId = null)
        {
            if (!sent.Add(key)) return;
            db.RemindersSent.Add(new ReminderSent { Key = key, SentAt = now });
            await notify.NotifyPeopleAsync(personIds, "reminder", title, JsonSerializer.Serialize(args, Json.Options), link, subjectType, subjectId);
        }

        // Advances: 3 days before due, on the due day, then daily while overdue.
        var cleared = await db.Set<MoneyRequest>().AsNoTracking().Where(r => r.Kind == MoneyRequestKind.AdvanceClearance && r.Status != MoneyRequestStatus.Cancelled && r.Status != MoneyRequestStatus.Rejected)
            .Select(r => r.RelatedAdvanceId).ToListAsync(ct);
        foreach (var a in await db.Set<MoneyRequest>().AsNoTracking().Where(r => r.Kind == MoneyRequestKind.Advance && r.Status == MoneyRequestStatus.Settled && r.AdvanceDueDate != null).ToListAsync(ct))
        {
            if (cleared.Contains(a.Id)) continue;
            var days = a.AdvanceDueDate!.Value.DayNumber - today.DayNumber;
            if (days == 3) await Once($"adv:{a.Id}:3", [a.RequesterPersonId], "notify.reminder.advanceSoon", new { a.Purpose, due = a.AdvanceDueDate }, $"#/finance/requests/{a.Id}", nameof(MoneyRequest), a.Id);
            if (days <= 0) await Once($"adv:{a.Id}:{today:yyyyMMdd}", [a.RequesterPersonId], days == 0 ? "notify.reminder.advanceDue" : "notify.reminder.advanceOverdue",
                new { a.Purpose, due = a.AdvanceDueDate, days = -days }, $"#/finance/requests/{a.Id}", nameof(MoneyRequest), a.Id);
        }

        // Progress reports due within 14 days and on the day.
        foreach (var r in await db.Set<ProgressReport>().AsNoTracking().Where(r => r.RecordStatus == RecordStatus.Active && r.SubmittedDate == null && r.DueDate != null
                     && r.DueDate <= today.AddDays(14)).ToListAsync(ct))
        {
            var leads = await db.Set<ProjectMember>().AsNoTracking().Where(m => m.ProjectId == r.ProjectId && (m.Role == ProjectRole.PI || m.Role == ProjectRole.CoPI || m.Role == ProjectRole.Coordinator))
                .Select(m => m.PersonId).ToListAsync(ct);
            var d = r.DueDate!.Value.DayNumber - today.DayNumber;
            var bucket = d <= 0 ? "due" : d <= 3 ? "3" : "14";
            await Once($"rpt:{r.Id}:{bucket}", leads, "notify.reminder.report", new { kind = r.Kind, due = r.DueDate, r.ProjectId }, $"#/projects/{r.ProjectId}", nameof(ProgressReport), r.Id);
        }

        // Meetings: a day before and an hour before.
        foreach (var m in await db.Set<Meeting>().AsNoTracking().Where(m => m.RecordStatus == RecordStatus.Active && m.Status == MeetingStatus.Scheduled
                     && m.StartAt > now && m.StartAt <= now.AddHours(25)).ToListAsync(ct))
        {
            var people = await db.Set<MeetingAttendee>().AsNoTracking().Where(a => a.MeetingId == m.Id && a.PersonId != null && a.Response != AttendeeResponse.Declined)
                .Select(a => a.PersonId!).ToListAsync(ct);
            people.Add(m.OrganizerPersonId);
            var bucket = m.StartAt <= now.AddMinutes(75) ? "1h" : "1d";
            await Once($"mtg:{m.Id}:{bucket}", people, "notify.reminder.meeting", new { m.Title, start = m.StartAt, m.Location }, $"#/meetings/{m.Id}", nameof(Meeting), m.Id);
        }

        // Bookings an hour before.
        foreach (var b in await db.Set<Booking>().AsNoTracking().Where(b => b.RecordStatus == RecordStatus.Active && b.Status == BookingStatus.Confirmed
                     && b.StartAt > now && b.StartAt <= now.AddMinutes(75)).ToListAsync(ct))
            await Once($"bkg:{b.Id}", [b.BookerPersonId], "notify.reminder.booking", new { b.Title, start = b.StartAt, b.ResourceId }, "#/bookings", nameof(Booking), b.Id);

        // Tasks due tomorrow.
        foreach (var t in await db.Set<TaskItem>().AsNoTracking().Where(t => t.RecordStatus == RecordStatus.Active && t.DueDate == today.AddDays(1)
                     && t.Status != TaskState.Done && t.Status != TaskState.Cancelled).ToListAsync(ct))
        {
            var who = await db.Set<TaskAssignee>().AsNoTracking().Where(a => a.TaskId == t.Id && a.RecordStatus == RecordStatus.Active).Select(a => a.PersonId).ToListAsync(ct);
            if (who.Count == 0) who.Add(t.CreatorPersonId);
            await Once($"tsk:{t.Id}:{t.DueDate:yyyyMMdd}", who, "notify.reminder.task", new { t.Title, due = t.DueDate }, $"#/tasks/{t.Id}", nameof(TaskItem), t.Id);
        }

        // Calibration / maintenance due within 14 days → custodian.
        foreach (var mnt in await db.Set<Maintenance>().AsNoTracking().Where(m => m.RecordStatus == RecordStatus.Active && m.NextDueDate != null && m.NextDueDate <= today.AddDays(14)).ToListAsync(ct))
        {
            var custodian = await db.Set<Asset>().AsNoTracking().Where(a => a.Id == mnt.AssetId).Select(a => a.CustodianPersonId).FirstOrDefaultAsync(ct);
            if (custodian != null)
                await Once($"mnt:{mnt.Id}:{mnt.NextDueDate:yyyyMMdd}", [custodian], "notify.reminder.maintenance", new { mnt.AssetId, due = mnt.NextDueDate }, $"#/assets/{mnt.AssetId}", nameof(Asset), mnt.AssetId);
        }

        // Short-term accounts: warn 14 days ahead, then expire and end their sessions.
        foreach (var u in await db.Set<UserAccount>().Where(u => u.Status == AccountStatus.Active && u.ExpiresAt != null && u.ExpiresAt <= today.AddDays(14)).ToListAsync(ct))
        {
            if (u.ExpiresAt < today)
            {
                u.Status = AccountStatus.Expired;
                foreach (var s in await db.Set<Session>().Where(s => s.UserId == u.Id && s.RevokedAt == null).ToListAsync(ct)) { s.RevokedAt = now; s.RevokeReason = "expired"; }
                continue;
            }
            await Once($"exp:{u.Id}:{u.ExpiresAt:yyyyMMdd}", [u.PersonId], "notify.reminder.accountExpiring", new { expires = u.ExpiresAt }, "#/profile");
            if (!sent.Contains($"expadm:{u.Id}:{u.ExpiresAt:yyyyMMdd}"))
            {
                sent.Add($"expadm:{u.Id}:{u.ExpiresAt:yyyyMMdd}");
                db.RemindersSent.Add(new ReminderSent { Key = $"expadm:{u.Id}:{u.ExpiresAt:yyyyMMdd}", SentAt = now });
                await notify.NotifyUserManagersAsync("reminder", "notify.reminder.accountExpiringAdmin",
                    JsonSerializer.Serialize(new { u.Username, expires = u.ExpiresAt }, Json.Options), "#/admin/users", "account.expiring");
            }
        }

        // CVs of applicants not taken: deleted after the retention period (ADR 0007).
        var retention = int.TryParse((await db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(x => x.Key == "recruitment.cvRetentionDays", ct))?.Value, out var rd) ? rd : 365;
        var cutoff = now.AddDays(-retention);
        foreach (var app in await db.Set<Application>().Where(a => a.CvFileId != null && a.SubmittedAt < cutoff
                     && (a.Status == ApplicationStatus.Rejected || a.Status == ApplicationStatus.Withdrawn)).ToListAsync(ct))
        {
            var blob = await db.Set<FileBlob>().FirstOrDefaultAsync(b => b.Id == app.CvFileId, ct);
            if (blob != null)
            {
                var opt = scope.ServiceProvider.GetRequiredService<AppOptions>();
                var path = Path.Combine(opt.UploadsPath, blob.StoredName);
                if (File.Exists(path)) File.Delete(path);
                blob.RecordStatus = RecordStatus.Deleted;
            }
            app.CvFileId = null;
        }

        await db.SaveChangesAsync(ct);
    }
}
