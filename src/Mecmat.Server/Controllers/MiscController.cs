using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Finance;
using Mecmat.Services;

namespace Mecmat.Controllers;

[ApiController, Route("api")]
public sealed class MiscController(AppDbContext db, CurrentUser cu, IHttpClientFactory http, FinanceService fin) : ControllerBase
{
    private void RequireAuth()
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
    }

    /// <summary>Full value of an encrypted field. Every reveal is logged against the person whose
    /// data it is (ADR 0007).</summary>
    [HttpGet("sensitive/{kind}/{id}/{field}")]
    public async Task<IActionResult> Reveal(string kind, string id, string field)
    {
        RequireAuth();
        string? personId, value;
        switch (kind, field)
        {
            case ("people", "nationalId"):
                var p = await db.Set<Person>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
                personId = p.Id; value = p.NationalId; break;
            case ("payout-channels", "accountNo" or "promptPayId"):
                var c = await db.Set<PayoutChannel>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
                personId = c.PersonId; value = field == "accountNo" ? c.AccountNo : c.PromptPayId; break;
            default:
                throw new AppException("errors.notFound", 404);
        }
        if (!cu.Can(Modules.Sensitive, PermAction.View, personId == cu.PersonId)) throw new AppException("errors.forbidden", 403);
        if (personId != cu.PersonId)
        {
            db.SensitiveAccessLogs.Add(new SensitiveAccessLog { At = DateTime.UtcNow, ViewerUserId = cu.UserId!, SubjectPersonId = personId, FieldKey = kind + "." + field });
            await db.SaveChangesAsync();
        }
        return Ok(new { value });
    }

    /// <summary>Looks a DOI up on Crossref (free, no key) so the publication form fills itself.
    /// The result is only a suggestion; nothing is saved here.</summary>
    [HttpGet("doi")]
    public async Task<IActionResult> DoiLookup([FromQuery] string doi)
    {
        RequireAuth();
        var d = Services.Doi.Normalise(doi);
        if (string.IsNullOrEmpty(d) || !d.StartsWith("10.", StringComparison.Ordinal) || d.Length > 200) throw new AppException("publications.doiInvalid");
        var client = http.CreateClient("crossref");
        using var res = await client.GetAsync("works/" + Uri.EscapeDataString(d));
        if (!res.IsSuccessStatusCode) throw new AppException("publications.doiNotFound", 404);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var m = doc.RootElement.GetProperty("message");
        static string? S(JsonElement e, string n) => e.TryGetProperty(n, out var v) ? v.ValueKind == JsonValueKind.Array ? (v.GetArrayLength() > 0 ? v[0].GetString() : null) : v.ToString() : null;
        int? year = null;
        foreach (var k in new[] { "published-print", "published-online", "issued" })
            if (m.TryGetProperty(k, out var pd) && pd.TryGetProperty("date-parts", out var parts) && parts[0].GetArrayLength() > 0) { year = parts[0][0].GetInt32(); break; }
        var authors = m.TryGetProperty("author", out var a)
            ? a.EnumerateArray().Select(x => new
            {
                name = $"{S(x, "given")} {S(x, "family")}".Trim(),
                orcid = S(x, "ORCID")?.Replace("http://orcid.org/", "").Replace("https://orcid.org/", ""),
                affiliation = x.TryGetProperty("affiliation", out var af) && af.GetArrayLength() > 0 ? S(af[0], "name") : null,
            }).ToList()
            : [];
        var type = S(m, "type");
        return Ok(new
        {
            doi = d, titleEn = S(m, "title"), venueName = S(m, "container-title"), volume = S(m, "volume"), issue = S(m, "issue"),
            pages = S(m, "page"), articleNumber = S(m, "article-number"), year, publisher = S(m, "publisher"), issn = S(m, "ISSN"),
            isbn = S(m, "ISBN"), url = "https://doi.org/" + d, citationCount = m.TryGetProperty("is-referenced-by-count", out var cc) ? cc.GetInt32() : (int?)null,
            type = type switch
            {
                "proceedings-article" => nameof(PublicationType.ConferencePaper), "book-chapter" => nameof(PublicationType.BookChapter),
                "book" or "monograph" => nameof(PublicationType.Book), "report" => nameof(PublicationType.ResearchReport),
                _ => nameof(PublicationType.JournalArticle),
            },
            authors,
        });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] int take = 200)
    {
        RequireAuth();
        var canAll = cu.CanAll(Modules.Audit, PermAction.View);
        var q = db.ChangeLogs.AsNoTracking();
        if (entityType != null) q = q.Where(l => l.EntityType == entityType);
        if (entityId != null) q = q.Where(l => l.EntityId == entityId);
        // Without the Audit module a member still sees the history of one record they can open —
        // the CRUD rules decide that, so here only their own actions are listed.
        if (!canAll) q = q.Where(l => l.UserId == cu.UserId);
        var rows = await q.OrderByDescending(l => l.Id).Take(Math.Clamp(take, 1, 1000)).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("sensitive-log/{personId}")]
    public async Task<IActionResult> SensitiveLog(string personId)
    {
        RequireAuth();
        if (personId != cu.PersonId && !cu.CanAll(Modules.Audit, PermAction.View)) throw new AppException("errors.forbidden", 403);
        return Ok(await db.SensitiveAccessLogs.AsNoTracking().Where(l => l.SubjectPersonId == personId).OrderByDescending(l => l.Id).Take(200).ToListAsync());
    }

    /// <summary>One feed for the calendar page: meetings, bookings, due dates — filtered to what the
    /// viewer may see.</summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        RequireAuth();
        if (to <= from || (to - from).TotalDays > 120) throw new AppException("bookings.rangeInvalid");
        from = from.ToUniversalTime(); to = to.ToUniversalTime();
        var s = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        var (df, dt) = (DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(from, Clock.Bangkok)), DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(to, Clock.Bangkok)));
        var events = new List<object>();
        var allMeetings = cu.CanAll(Modules.Meetings, PermAction.View);
        foreach (var m in await db.Set<Meeting>().AsNoTracking().Where(m => m.RecordStatus == RecordStatus.Active && m.StartAt < to && m.EndAt > from && m.Status != MeetingStatus.Cancelled).ToListAsync())
            if (allMeetings || s.MeetingIds.Contains(m.Id))
                events.Add(new { kind = "meeting", m.Id, title = m.Title, start = m.StartAt, end = m.EndAt, m.Location, link = $"#/meetings/{m.Id}" });
        foreach (var b in await db.Set<Booking>().AsNoTracking().Where(b => b.RecordStatus == RecordStatus.Active && b.BookerPersonId == cu.PersonId
                     && b.StartAt < to && b.EndAt > from && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)).ToListAsync())
            events.Add(new { kind = "booking", b.Id, title = b.Title, start = b.StartAt, end = b.EndAt, b.Status, b.ResourceId, link = "#/bookings" });
        var allTasks = cu.CanAll(Modules.Tasks, PermAction.View);
        foreach (var t in await db.Set<TaskItem>().AsNoTracking().Where(t => t.RecordStatus == RecordStatus.Active && t.DueDate >= df && t.DueDate <= dt && t.Status != TaskState.Done && t.Status != TaskState.Cancelled).ToListAsync())
            if (allTasks || s.TaskIds.Contains(t.Id))
                events.Add(new { kind = "task", t.Id, title = t.Title, date = t.DueDate, link = $"#/tasks/{t.Id}" });
        var allProjects = cu.CanAll(Modules.Projects, PermAction.View);
        foreach (var ms in await db.Set<Milestone>().AsNoTracking().Where(x => x.RecordStatus == RecordStatus.Active && x.DueDate >= df && x.DueDate <= dt && x.Status == MilestoneStatus.Pending).ToListAsync())
            if (allProjects || s.ProjectIds.Contains(ms.ProjectId))
                events.Add(new { kind = "milestone", ms.Id, title = ms.Name, date = ms.DueDate, ms.ProjectId, link = $"#/projects/{ms.ProjectId}" });
        foreach (var r in await db.Set<ProgressReport>().AsNoTracking().Where(x => x.RecordStatus == RecordStatus.Active && x.DueDate >= df && x.DueDate <= dt && x.SubmittedDate == null).ToListAsync())
            if (allProjects || s.ProjectIds.Contains(r.ProjectId))
                events.Add(new { kind = "report", r.Id, title = r.Kind.ToString(), date = r.DueDate, r.ProjectId, link = $"#/projects/{r.ProjectId}" });
        foreach (var a in await db.Set<MoneyRequest>().AsNoTracking().Where(x => x.Kind == MoneyRequestKind.Advance && x.Status == MoneyRequestStatus.Settled
                     && x.RequesterPersonId == cu.PersonId && x.AdvanceDueDate >= df && x.AdvanceDueDate <= dt).ToListAsync())
            events.Add(new { kind = "advance", a.Id, title = a.Purpose, date = a.AdvanceDueDate, link = $"#/finance/requests/{a.Id}" });
        foreach (var rc in await db.Set<Recruitment>().AsNoTracking().Where(x => x.RecordStatus == RecordStatus.Active && x.Status == RecruitmentStatus.Open && x.CloseDate >= df && x.CloseDate <= dt).ToListAsync())
            events.Add(new { kind = "recruitment", rc.Id, title = rc.TitleTh, date = rc.CloseDate, link = "#/recruitments" });
        return Ok(events);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        RequireAuth();
        var s = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!);
        var active = RecordStatus.Active;
        var today = Clock.TodayBangkok();
        var groupView = cu.CanAll(Modules.Dashboard, PermAction.View);
        var thisYear = today.Year;

        var pubs = await db.Set<Publication>().AsNoTracking().Where(p => p.RecordStatus == active && p.Stage == PublicationStage.Published)
            .Select(p => new { p.Id, p.Year, p.Type }).ToListAsync();
        var indexed = await db.Set<PublicationIndexing>().AsNoTracking().Where(i => i.RecordStatus == active)
            .Select(i => new { i.PublicationId, i.Database, i.Quartile }).ToListAsync();
        var projects = await db.Set<Project>().AsNoTracking().Where(p => p.RecordStatus == active)
            .Select(p => new { p.Id, p.Status, p.TotalBudget, p.EndDate }).ToListAsync();
        var outputs = await db.Set<ProjectOutput>().AsNoTracking().Where(o => o.RecordStatus == active)
            .Select(o => new { o.ProjectId, o.OutputType, o.TargetQty, o.ActualQty }).ToListAsync();
        var myTasks = await db.Set<TaskItem>().AsNoTracking().Where(t => t.RecordStatus == active && s.TaskIds.Contains(t.Id)
                && t.Status != TaskState.Done && t.Status != TaskState.Cancelled)
            .OrderBy(t => t.DueDate).Take(10).Select(t => new { t.Id, t.Title, t.DueDate, t.Status, t.Priority, t.ProjectId }).ToListAsync();
        var nowUtc = DateTime.UtcNow;
        var myMeetings = await db.Set<Meeting>().AsNoTracking().Where(m => m.RecordStatus == active && s.MeetingIds.Contains(m.Id) && m.EndAt >= nowUtc && m.Status == MeetingStatus.Scheduled)
            .OrderBy(m => m.StartAt).Take(5).Select(m => new { m.Id, m.Title, m.StartAt, m.EndAt, m.Location }).ToListAsync();
        var myBookings = await db.Set<Booking>().AsNoTracking().Where(b => b.RecordStatus == active && b.BookerPersonId == cu.PersonId && b.EndAt >= nowUtc
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
            .OrderBy(b => b.StartAt).Take(5).Select(b => new { b.Id, b.Title, b.StartAt, b.EndAt, b.ResourceId, b.Status }).ToListAsync();
        var myRequests = await db.Set<MoneyRequest>().AsNoTracking().Where(r => r.RequesterPersonId == cu.PersonId && r.RecordStatus == active
                && r.Status != MoneyRequestStatus.Settled && r.Status != MoneyRequestStatus.Cancelled && r.Status != MoneyRequestStatus.Rejected)
            .OrderByDescending(r => r.CreatedAt).Take(5).Select(r => new { r.Id, r.Kind, r.Purpose, r.TotalAmount, r.Status }).ToListAsync();

        object? finance = null;
        if (cu.CanAny(Modules.Finance, PermAction.View))
        {
            var tree = await fin.TreeAsync(s);
            var roots = tree.Where(n => n.ParentAccountId == null || tree.All(t => t.Id != n.ParentAccountId)).ToList();
            finance = new { balance = roots.Sum(r => r.Balance), committed = roots.Sum(r => r.Committed), budget = roots.Sum(r => r.Budget), spent = roots.Sum(r => r.SpentThisYear), accounts = roots };
        }

        var mine = s.PublicationIds;
        var pubSet = groupView ? pubs : pubs.Where(p => mine.Contains(p.Id)).ToList();
        return Ok(new
        {
            groupView,
            people = groupView ? await db.Set<Person>().AsNoTracking().Where(p => p.RecordStatus == active && p.Kind == PersonKind.Internal && (p.LeftDate == null || p.LeftDate >= today))
                .GroupBy(p => p.Category).Select(g => new { category = g.Key, count = g.Count() }).ToListAsync() : null,
            publications = new
            {
                total = pubSet.Count,
                thisYear = pubSet.Count(p => p.Year == thisYear),
                byYear = pubSet.Where(p => p.Year != null).GroupBy(p => p.Year!.Value).OrderBy(g => g.Key).Select(g => new { year = g.Key, count = g.Count() }),
                byType = pubSet.GroupBy(p => p.Type).Select(g => new { type = g.Key, count = g.Count() }),
                scopusQ1Q2 = pubSet.Count(p => indexed.Any(i => i.PublicationId == p.Id && i.Database == IndexDatabase.Scopus && i.Quartile is "Q1" or "Q2")),
            },
            projects = new
            {
                active = projects.Count(p => p.Status is ProjectStatus.Active or ProjectStatus.Extended or ProjectStatus.Funded && (groupView || s.ProjectIds.Contains(p.Id))),
                byStatus = projects.Where(p => groupView || s.ProjectIds.Contains(p.Id)).GroupBy(p => p.Status).Select(g => new { status = g.Key, count = g.Count() }),
                totalBudget = projects.Where(p => groupView || s.ProjectIds.Contains(p.Id)).Sum(p => p.TotalBudget ?? 0),
                endingSoon = projects.Count(p => p.EndDate >= today && p.EndDate <= today.AddDays(60) && (groupView || s.ProjectIds.Contains(p.Id))),
            },
            outputs = outputs.Where(o => groupView || s.ProjectIds.Contains(o.ProjectId)).GroupBy(o => o.OutputType)
                .Select(g => new { type = g.Key, target = g.Sum(o => o.TargetQty ?? 0), actual = g.Sum(o => o.ActualQty ?? 0) }),
            finance,
            my = new { tasks = myTasks, meetings = myMeetings, bookings = myBookings, requests = myRequests },
        });
    }
}
