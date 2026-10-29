using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Bookings;

[ApiController, Route("api/bookings")]
public sealed class BookingsController(AppDbContext db, CurrentUser cu, ApprovalEngine approvals, NotificationService notify) : ControllerBase
{
    public sealed record BookBody(string ResourceId, DateTime StartAt, DateTime EndAt, string Title, string? ProjectId, string? Note, int? RepeatWeeks);
    public sealed record CancelBody(string? Reason);

    private static readonly BookingStatus[] Holding = [BookingStatus.Pending, BookingStatus.Confirmed];

    private void RequireAuth()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
    }

    /// <summary>Calendar feed. Everyone who may book sees when a resource is taken (and by whom, so
    /// people can sort it out between themselves); title, note and project stay with the booker,
    /// their project and the resource manager (docs/authorization.md).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? resourceId)
    {
        RequireAuth();
        if (!cu.CanAny(Modules.Bookings, PermAction.View) && !cu.CanAny(Modules.Bookings, PermAction.Create)) throw new AppException("errors.forbidden", 403);
        if (to <= from || (to - from).TotalDays > 120) throw new AppException("bookings.rangeInvalid");
        var scope = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        from = from.ToUniversalTime(); to = to.ToUniversalTime();
        var q = db.Set<Booking>().AsNoTracking().Where(b => b.RecordStatus == RecordStatus.Active && b.StartAt < to && b.EndAt > from
                                                           && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Rejected);
        if (resourceId != null) q = q.Where(b => b.ResourceId == resourceId);
        var rows = await q.OrderBy(b => b.StartAt).ToListAsync();
        var blackouts = await db.Set<ResourceBlackout>().AsNoTracking()
            .Where(x => x.RecordStatus == RecordStatus.Active && x.StartAt < to && x.EndAt > from && (resourceId == null || x.ResourceId == resourceId))
            .ToListAsync();
        var people = await PeopleNamesAsync(rows.Select(r => r.BookerPersonId));
        var seeAll = cu.CanAll(Modules.Bookings, PermAction.Edit);
        return Ok(new
        {
            bookings = rows.Select(b =>
            {
                var mine = b.BookerPersonId == cu.PersonId;
                var detail = mine || seeAll || scope.ManagedResourceIds.Contains(b.ResourceId) || (b.ProjectId != null && scope.ProjectIds.Contains(b.ProjectId));
                return new
                {
                    b.Id, b.ResourceId, b.StartAt, b.EndAt, b.Status, b.BookerPersonId, booker = people.GetValueOrDefault(b.BookerPersonId),
                    title = detail ? b.Title : null, note = detail ? b.Note : null, projectId = detail ? b.ProjectId : null,
                    b.RecurrenceGroupId, mine,
                    canCancel = mine || seeAll || scope.ManagedResourceIds.Contains(b.ResourceId),
                };
            }),
            blackouts,
        });
    }

    private async Task<Dictionary<string, string>> PeopleNamesAsync(IEnumerable<string> ids)
    {
        var list = ids.Distinct().ToList();
        return await db.Set<Person>().AsNoTracking().Where(p => list.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => (p.Nickname != null ? p.Nickname + " · " : "") + p.FirstNameTh + " " + p.LastNameTh);
    }

    [HttpPost]
    public async Task<IActionResult> Book(BookBody b)
    {
        RequireAuth();
        if (!cu.CanAny(Modules.Bookings, PermAction.Create)) throw new AppException("errors.forbidden", 403);
        var res = await db.Set<Resource>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == b.ResourceId && r.RecordStatus == RecordStatus.Active)
                  ?? throw new AppException("errors.notFound", 404);
        if (!res.IsActive) throw new AppException("bookings.resourceInactive");
        if (string.IsNullOrWhiteSpace(b.Title)) throw new AppException("bookings.titleRequired");
        var weeks = Math.Clamp(b.RepeatWeeks ?? 1, 1, 26);
        var start = b.StartAt.ToUniversalTime();
        var end = b.EndAt.ToUniversalTime();
        Validate(res, start, end);

        var created = new List<Booking>();
        await db.ExclusiveAsync(async () =>
        {
            var group = weeks > 1 ? Tokens.NewToken(9) : null;
            for (var w = 0; w < weeks; w++)
            {
                var s = start.AddDays(7 * w);
                var e = end.AddDays(7 * w);
                if (w > 0) Validate(res, s, e, checkAdvance: false);
                await RequireFreeAsync(res.Id, s, e, null);
                created.Add(new Booking
                {
                    ResourceId = res.Id, BookerPersonId = cu.PersonId!, ProjectId = b.ProjectId, Title = b.Title.Trim(), Note = b.Note?.Trim(),
                    StartAt = s, EndAt = e, RecurrenceGroupId = group,
                    Status = res.RequiresApproval ? BookingStatus.Pending : BookingStatus.Confirmed,
                });
            }
            db.AddRange(created);
            await db.SaveChangesAsync();
            if (res.RequiresApproval)
                foreach (var bk in created)
                {
                    var inst = await approvals.SubmitAsync(new ApprovalRequest(nameof(Booking), bk.Id, bk.BookerPersonId, ResourceId: res.Id, ProjectId: bk.ProjectId));
                    bk.ApprovalInstanceId = inst.Id;
                }
            await db.SaveChangesAsync();
        });
        return Ok(new { ids = created.Select(c => c.Id), status = created[0].Status });
    }

    private static void Validate(Resource res, DateTime start, DateTime end, bool checkAdvance = true)
    {
        if (end <= start) throw new AppException("errors.endBeforeStart");
        var minutes = (end - start).TotalMinutes;
        if (res.MinDurationMinutes is { } min && minutes < min) throw new AppException("bookings.tooShort", 400, new { min });
        if (res.MaxDurationMinutes is { } max && minutes > max) throw new AppException("bookings.tooLong", 400, new { max });
        if (checkAdvance && start < DateTime.UtcNow.AddMinutes(-5)) throw new AppException("bookings.inPast");
        if (checkAdvance && res.MaxAdvanceDays is { } adv && start > DateTime.UtcNow.AddDays(adv)) throw new AppException("bookings.tooFarAhead", 400, new { days = adv });
        var ls = TimeZoneInfo.ConvertTimeFromUtc(start, Clock.Bangkok);
        var le = TimeZoneInfo.ConvertTimeFromUtc(end, Clock.Bangkok);
        if (res.SlotMinutes > 0 && ((int)ls.TimeOfDay.TotalMinutes % res.SlotMinutes != 0 || (int)minutes % res.SlotMinutes != 0))
            throw new AppException("bookings.slotAlign", 400, new { slot = res.SlotMinutes });
        if (res.OpenHoursJson is { Length: > 0 } json)
        {
            var hours = JsonSerializer.Deserialize<string[]?[]>(json);
            if (hours is { Length: 7 })
            {
                if (ls.Date != le.AddTicks(-1).Date) throw new AppException("bookings.oneDay");
                var day = hours[((int)ls.DayOfWeek + 6) % 7]; // index 0 = Monday
                if (day is not { Length: 2 }) throw new AppException("bookings.closedDay");
                var open = TimeSpan.Parse(day[0]);
                var close = TimeSpan.Parse(day[1]);
                if (ls.TimeOfDay < open || (le.TimeOfDay > close && le.TimeOfDay != TimeSpan.Zero) || (le.TimeOfDay == TimeSpan.Zero && close < TimeSpan.FromHours(24)))
                    throw new AppException("bookings.outsideHours", 400, new { open = day[0], close = day[1] });
            }
        }
    }

    private async Task RequireFreeAsync(string resourceId, DateTime start, DateTime end, string? exceptId)
    {
        var clash = await db.Set<Booking>().AsNoTracking().FirstOrDefaultAsync(x => x.ResourceId == resourceId && x.Id != exceptId
            && x.RecordStatus == RecordStatus.Active && Holding.Contains(x.Status) && x.StartAt < end && x.EndAt > start);
        if (clash != null) throw new AppException("bookings.clash", 409, new { clash.StartAt, clash.EndAt });
        var closed = await db.Set<ResourceBlackout>().AsNoTracking().AnyAsync(x => x.ResourceId == resourceId
            && x.RecordStatus == RecordStatus.Active && x.StartAt < end && x.EndAt > start);
        if (closed) throw new AppException("bookings.blackout", 409);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancelBody b, [FromQuery] bool series = false)
    {
        RequireAuth();
        var bk = await db.Set<Booking>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        var managed = await db.Set<Resource>().AnyAsync(r => r.Id == bk.ResourceId && r.ManagerPersonId == cu.PersonId);
        var mine = bk.BookerPersonId == cu.PersonId;
        if (!mine && !managed && !cu.CanAll(Modules.Bookings, PermAction.Edit)) throw new AppException("errors.forbidden", 403);
        var targets = series && bk.RecurrenceGroupId != null
            ? await db.Set<Booking>().Where(x => x.RecurrenceGroupId == bk.RecurrenceGroupId && x.StartAt >= bk.StartAt).ToListAsync()
            : [bk];
        foreach (var t in targets.Where(t => Holding.Contains(t.Status)))
        {
            t.Status = BookingStatus.Cancelled;
            t.CancelledReason = b.Reason;
            if (t.ApprovalInstanceId != null)
                await approvals.CancelAsync(await db.Set<ApprovalInstance>().FirstAsync(i => i.Id == t.ApprovalInstanceId));
        }
        if (!mine)
            await notify.NotifyPeopleAsync([bk.BookerPersonId], "booking.cancelled", "notify.booking.cancelled",
                JsonSerializer.Serialize(new { title = bk.Title, start = bk.StartAt, reason = b.Reason }, Json.Options), "#/bookings", nameof(Booking), bk.Id);
        await db.SaveChangesAsync();
        return Ok(new { ok = true, count = targets.Count });
    }
}

public sealed class BookingApprovalHandler : IApprovalHandler
{
    public string SubjectType => nameof(Booking);

    public async Task OnClosedAsync(AppDbContext db, ApprovalInstance inst)
    {
        var b = await db.Set<Booking>().FirstAsync(x => x.Id == inst.SubjectId);
        if (b.Status != BookingStatus.Pending) return;
        b.Status = inst.Status == ApprovalStatus.Approved ? BookingStatus.Confirmed : BookingStatus.Rejected;
    }
}
