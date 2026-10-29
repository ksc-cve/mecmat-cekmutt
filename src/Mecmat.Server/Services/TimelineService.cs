using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

public sealed record TimelineItem(
    string Kind, string Date, string? EndDate, bool YearOnly, string TitleTh, string? TitleEn,
    string? SubTh, string? SubEn, string? Url, string? Link, string SourceType, string SourceId);

public sealed record NetworkPerson(string Id, string NameTh, string NameEn, string? Slug, string? PhotoFileId, string Relation, int Count, string? Detail);
public sealed record NetworkOrg(string Id, string NameTh, string? NameEn, string? Country, OrganizationKind Kind, int Count);

public sealed record CareerView(List<TimelineItem> Items, List<NetworkPerson> Mentors, List<NetworkPerson> Students,
    List<NetworkPerson> CoAuthors, List<NetworkOrg> Organizations);

/// <summary>Builds a person's career timeline and network from everything already recorded about
/// them, so nothing is entered twice: a degree added under Education or a paper added under
/// Publications appears on the timeline by itself. With publicOnly, only what the person chose to
/// show (ADR 0007) and only public records are used.</summary>
public sealed class TimelineService(AppDbContext db)
{
    private const RecordStatus Active = RecordStatus.Active;

    public async Task<CareerView> BuildAsync(string personId, bool publicOnly, IReadOnlySet<string>? shown = null)
    {
        bool Show(string key) => !publicOnly || (shown?.Contains(key) ?? false);
        var items = new List<TimelineItem>();
        var orgNames = await db.Set<Organization>().AsNoTracking().ToDictionaryAsync(o => o.Id);
        string? OrgTh(string? id) => id != null && orgNames.TryGetValue(id, out var o) ? o.NameTh : null;
        string? OrgEn(string? id) => id != null && orgNames.TryGetValue(id, out var o) ? o.NameEn ?? o.NameTh : null;

        if (Show("education"))
            foreach (var e in await db.Set<PersonEducation>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && (!publicOnly || x.IsPublic)).ToListAsync())
            {
                var year = e.GraduatedYear ?? e.StartYear;
                if (year == null) continue;
                items.Add(new TimelineItem("education", $"{year:0000}-12-31", e.StartYear != null && e.GraduatedYear != null ? null : null, true,
                    Join(e.DegreeNameTh ?? e.DegreeNameEn, e.Field), Join(e.DegreeNameEn ?? e.DegreeNameTh, e.Field),
                    Join(e.Institution, e.Country) + (e.StartYear != null && e.GraduatedYear != null && e.StartYear != e.GraduatedYear ? $" · {e.StartYear}–{e.GraduatedYear}" : ""),
                    null, null, null, nameof(PersonEducation), e.Id));
                if (!string.IsNullOrEmpty(e.ThesisTitle))
                    items[^1] = items[^1] with { SubTh = items[^1].SubTh + "\n" + e.ThesisTitle, SubEn = (items[^1].SubEn ?? items[^1].SubTh) + "\n" + e.ThesisTitle };
            }

        if (Show("employment"))
        {
            foreach (var w in await db.Set<PersonEmployment>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && (!publicOnly || x.IsPublic)).ToListAsync())
                items.Add(new TimelineItem("position", Iso(w.StartDate) ?? "0000-01-01", Iso(w.EndDate), false, w.Position, w.Position,
                    w.OrganizationText ?? OrgTh(w.OrganizationId), w.OrganizationText ?? OrgEn(w.OrganizationId), null, null, nameof(PersonEmployment), w.Id));
            foreach (var a in await db.Set<AcademicAppointment>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && x.AppointedDate != null).ToListAsync())
                items.Add(new TimelineItem("rank", Iso(a.AppointedDate)!, null, false, "rank:" + a.Rank, "rank:" + a.Rank, a.FieldName, a.FieldName, null, null, nameof(AcademicAppointment), a.Id));
            var group = await db.Set<PositionAssignment>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && x.FromDate != null)
                .Join(db.Set<GroupPosition>().Where(g => !publicOnly || g.IsPublic), a => a.PositionId, g => g.Id, (a, g) => new { a, g }).ToListAsync();
            foreach (var x in group)
                items.Add(new TimelineItem("position", Iso(x.a.FromDate)!, Iso(x.a.ToDate), false, x.g.NameTh, x.g.NameEn ?? x.g.NameTh, "MECMAT", "MECMAT", null, null, nameof(PositionAssignment), x.a.Id));
        }

        if (Show("awards"))
            foreach (var a in await db.Set<AwardRecipient>().AsNoTracking().Where(r => r.PersonId == personId && r.RecordStatus == Active)
                         .Join(db.Set<Award>().Where(a => a.RecordStatus == Active && (!publicOnly || a.IsPublic)), r => r.AwardId, a => a.Id, (r, a) => a).ToListAsync())
                if (a.Date != null)
                    items.Add(new TimelineItem("award", Iso(a.Date)!, null, false, a.TitleTh, a.TitleEn ?? a.TitleTh, a.Issuer, a.Issuer, null, null, nameof(Award), a.Id));

        var authored = await db.Set<PublicationAuthor>().AsNoTracking().Where(a => a.PersonId == personId && a.RecordStatus == Active).Select(a => a.PublicationId).ToListAsync();
        var pubs = await db.Set<Publication>().AsNoTracking()
            .Where(p => authored.Contains(p.Id) && p.RecordStatus == Active && (!publicOnly || (p.IsPublic && p.Stage == PublicationStage.Published))).ToListAsync();
        if (Show("publications"))
            foreach (var p in pubs.Where(p => p.Year != null))
                items.Add(new TimelineItem("publication", p.PublishedDate != null ? Iso(p.PublishedDate)! : $"{p.Year:0000}-06-30", null, p.PublishedDate == null,
                    p.TitleEn, p.TitleEn, p.VenueName, p.VenueName, p.Doi != null ? "https://doi.org/" + p.Doi : p.Url, $"#/publications/{p.Id}", nameof(Publication), p.Id));

        if (Show("projects"))
        {
            var memberships = await db.Set<ProjectMember>().AsNoTracking().Where(m => m.PersonId == personId && m.RecordStatus == Active && (!publicOnly || m.IsPublic))
                .Join(db.Set<Project>().Where(p => p.RecordStatus == Active && (!publicOnly || p.IsPublic)), m => m.ProjectId, p => p.Id, (m, p) => new { m, p }).ToListAsync();
            foreach (var x in memberships)
            {
                var start = x.m.FromDate ?? x.p.StartDate;
                if (start == null) continue;
                items.Add(new TimelineItem("project", Iso(start)!, Iso(x.m.ToDate ?? x.p.ExtendedEndDate ?? x.p.EndDate), false,
                    x.p.TitleTh, x.p.TitleEn ?? x.p.TitleTh, "role:" + x.m.Role, "role:" + x.m.Role, null, $"#/projects/{x.p.Id}", nameof(Project), x.p.Id));
            }
        }

        var mentorships = await db.Set<PersonMentorship>().AsNoTracking()
            .Where(m => (m.StudentPersonId == personId || m.MentorPersonId == personId) && m.RecordStatus == Active && (!publicOnly || m.IsPublic)).ToListAsync();
        var peopleIds = mentorships.SelectMany(m => new[] { m.StudentPersonId, m.MentorPersonId }).Distinct().ToList();

        if (Show("timeline"))
        {
            foreach (var ev in await db.Set<PersonEvent>().AsNoTracking().Where(e => e.PersonId == personId && e.RecordStatus == Active && (!publicOnly || e.IsPublic)).ToListAsync())
            {
                var whereTh = Join(ev.OrganizationText ?? OrgTh(ev.OrganizationId), ev.Country);
                var whereEn = Join(ev.OrganizationText ?? OrgEn(ev.OrganizationId), ev.Country);
                var descEn = ev.DescriptionEn ?? ev.DescriptionTh;
                items.Add(new TimelineItem(ev.Kind.ToString().ToLowerInvariant(), Iso(ev.Date)!, Iso(ev.EndDate), ev.DateIsApproximate,
                    ev.TitleTh, ev.TitleEn ?? ev.TitleTh,
                    ev.DescriptionTh == null ? whereTh : Join(whereTh) + "\n" + ev.DescriptionTh,
                    descEn == null ? whereEn : Join(whereEn) + "\n" + descEn,
                    ev.Url, null, nameof(PersonEvent), ev.Id));
            }
            var news = await db.Set<NewsLink>().AsNoTracking().Where(l => l.TargetType == nameof(Person) && l.TargetId == personId && l.RecordStatus == Active)
                .Join(db.Set<NewsPost>().Where(n => n.Status == PublishStatus.Published && n.RecordStatus == Active), l => l.NewsPostId, n => n.Id, (l, n) => n).ToListAsync();
            foreach (var n in news)
                items.Add(new TimelineItem("news", Iso(DateOnly.FromDateTime(n.PublishedAt ?? n.CreatedAt))!, null, false, n.TitleTh, n.TitleEn ?? n.TitleTh,
                    n.SummaryTh, n.SummaryEn ?? n.SummaryTh, "/news/" + n.Slug, null, nameof(NewsPost), n.Id));
        }

        var names = await db.Set<Person>().AsNoTracking().Where(p => peopleIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        if (Show("mentorship"))
            foreach (var m in mentorships.Where(m => m.MentorPersonId == personId && (m.ToYear ?? m.FromYear) != null))
                if (names.TryGetValue(m.StudentPersonId, out var st))
                    items.Add(new TimelineItem("supervision", $"{(m.ToYear ?? m.FromYear):0000}-12-30", null, true,
                        PublicQueryService.DisplayName(st, true), PublicQueryService.DisplayName(st, false),
                        "mentor:" + m.Role + (m.Degree != null ? "|" + m.Degree : ""), "mentor:" + m.Role + (m.Degree != null ? "|" + m.Degree : ""),
                        null, $"#/people/{st.Id}", nameof(PersonMentorship), m.Id));

        NetworkPerson Np(Person p, string relation, int count, string? detail) =>
            new(p.Id, PublicQueryService.DisplayName(p, true), PublicQueryService.DisplayName(p, false),
                p.Kind == PersonKind.Internal ? p.PublicSlug : null, p.PhotoFileId, relation, count, detail);

        var mentors = !Show("mentorship") ? [] : mentorships.Where(m => m.StudentPersonId == personId && names.ContainsKey(m.MentorPersonId))
            .Select(m => Np(names[m.MentorPersonId], m.Role.ToString(), 1, $"{m.Degree}|{m.FromYear}|{m.ToYear}")).ToList();
        var students = !Show("mentorship") ? [] : mentorships.Where(m => m.MentorPersonId == personId && names.ContainsKey(m.StudentPersonId))
            .Select(m => Np(names[m.StudentPersonId], m.Role.ToString(), 1, $"{m.Degree}|{m.FromYear}|{m.ToYear}"))
            .OrderByDescending(n => n.Detail).ToList();

        var coAuthors = new List<NetworkPerson>();
        if (Show("network") || Show("publications"))
        {
            var pubIds = pubs.Select(p => p.Id).ToList();
            var co = await db.Set<PublicationAuthor>().AsNoTracking()
                .Where(a => pubIds.Contains(a.PublicationId) && a.PersonId != null && a.PersonId != personId && a.RecordStatus == Active)
                .GroupBy(a => a.PersonId!).Select(g => new { Id = g.Key, Count = g.Select(x => x.PublicationId).Distinct().Count() }).ToListAsync();
            var coIds = co.Select(c => c.Id).ToList();
            var coPeople = await db.Set<Person>().AsNoTracking().Where(p => coIds.Contains(p.Id) && p.RecordStatus == Active).ToDictionaryAsync(p => p.Id);
            coAuthors = co.Where(c => coPeople.ContainsKey(c.Id)).OrderByDescending(c => c.Count)
                .Select(c => Np(coPeople[c.Id], "CoAuthor", c.Count, null)).ToList();
        }

        var orgs = new List<NetworkOrg>();
        if (Show("network") || Show("employment"))
        {
            var counts = new Dictionary<string, int>();
            void Add(string? id) { if (id != null && orgNames.ContainsKey(id)) counts[id] = counts.GetValueOrDefault(id) + 1; }
            foreach (var id in await db.Set<PersonEmployment>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && (!publicOnly || x.IsPublic)).Select(x => x.OrganizationId).ToListAsync()) Add(id);
            foreach (var id in await db.Set<PersonEvent>().AsNoTracking().Where(x => x.PersonId == personId && x.RecordStatus == Active && (!publicOnly || x.IsPublic)).Select(x => x.OrganizationId).ToListAsync()) Add(id);
            var projIds = await db.Set<ProjectMember>().AsNoTracking().Where(m => m.PersonId == personId && m.RecordStatus == Active).Select(m => m.ProjectId).ToListAsync();
            foreach (var id in await db.Set<ProjectPartner>().AsNoTracking().Where(x => projIds.Contains(x.ProjectId) && x.RecordStatus == Active).Select(x => (string?)x.OrganizationId).ToListAsync()) Add(id);
            foreach (var id in await db.Set<Project>().AsNoTracking().Where(x => projIds.Contains(x.Id) && (!publicOnly || x.IsPublic)).Select(x => x.FunderOrganizationId).ToListAsync()) Add(id);
            orgs = counts.Select(c => orgNames[c.Key])
                .Select(o => new NetworkOrg(o.Id, o.NameTh, o.NameEn, o.Country, o.Kind, counts[o.Id])).OrderByDescending(o => o.Count).ToList();
        }

        return new CareerView(items.OrderByDescending(i => i.Date).ThenBy(i => i.Kind).ToList(), mentors, students, coAuthors, orgs);
    }

    private static string? Iso(DateOnly? d) => d?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private static string Join(params string?[] parts) => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
