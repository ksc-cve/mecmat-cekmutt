using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Services;

public sealed record PublicPerson(
    string Id, string Slug, string NameTh, string NameEn, PersonCategory Category, string? Position, string? PhotoFileId,
    string? Email, string? Phone, string? Office, string? LineId, string? ExpertiseTh, string? ExpertiseEn, string? BioTh, string? BioEn,
    string? Orcid, string? Scopus, string? Scholar, string? ResearchGate, string? Website, IReadOnlySet<string> Shown, int Order, string? GroupPositionTh, string? GroupPositionEn);

/// <summary>The only way public pages read data (ADR 0002, 0007). Every query here filters on
/// published/active state and on each person's own visibility choices, so a page cannot leak a
/// private field by forgetting a filter. Sensitive columns are never selected at all.</summary>
public sealed class PublicQueryService(AppDbContext db)
{
    private const RecordStatus Active = RecordStatus.Active;

    public async Task<Dictionary<string, string?>> SettingsAsync() =>
        await db.Set<SiteSetting>().AsNoTracking().Where(s => s.RecordStatus == Active).ToDictionaryAsync(s => s.Key, s => s.Value);

    public Task<SitePage?> PageAsync(string key) =>
        db.Set<SitePage>().AsNoTracking().FirstOrDefaultAsync(p => p.Key == key && p.RecordStatus == Active);

    public Task<List<ResearchArea>> AreasAsync() =>
        db.Set<ResearchArea>().AsNoTracking().Where(a => a.RecordStatus == Active).OrderBy(a => a.DisplayOrder).ToListAsync();

    /// <summary>Internal members still in the group, with only the fields they chose to show.</summary>
    public async Task<List<PublicPerson>> PeopleAsync()
    {
        var today = Clock.TodayBangkok();
        var people = await db.Set<Person>().AsNoTracking()
            .Where(p => p.RecordStatus == Active && p.Kind == PersonKind.Internal && p.PublicSlug != null && (p.LeftDate == null || p.LeftDate >= today))
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.FirstNameTh).ToListAsync();
        return await ShapeAsync(people);
    }

    public async Task<PublicPerson?> PersonAsync(string slug)
    {
        var p = await db.Set<Person>().AsNoTracking().FirstOrDefaultAsync(x => x.PublicSlug == slug && x.RecordStatus == Active && x.Kind == PersonKind.Internal);
        return p == null ? null : (await ShapeAsync([p])).FirstOrDefault();
    }

    private async Task<List<PublicPerson>> ShapeAsync(List<Person> people)
    {
        var ids = people.Select(p => p.Id).ToList();
        var vis = await db.Set<PublicFieldVisibility>().AsNoTracking()
            .Where(v => ids.Contains(v.PersonId) && v.IsPublic && v.RecordStatus == Active).ToListAsync();
        var today = Clock.TodayBangkok();
        var positions = await db.Set<PositionAssignment>().AsNoTracking()
            .Where(a => ids.Contains(a.PersonId) && a.RecordStatus == Active && (a.ToDate == null || a.ToDate >= today) && (a.FromDate == null || a.FromDate <= today))
            .Join(db.Set<GroupPosition>().Where(g => g.IsPublic && g.RecordStatus == Active), a => a.PositionId, g => g.Id, (a, g) => new { a.PersonId, g.NameTh, g.NameEn, g.Rank })
            .ToListAsync();
        return people.Select(p =>
        {
            var shown = vis.Where(v => v.PersonId == p.Id).Select(v => v.FieldKey).ToHashSet();
            string? S(string key, string? value) => shown.Contains(key) ? value : null;
            var pos = positions.Where(x => x.PersonId == p.Id).OrderBy(x => x.Rank).FirstOrDefault();
            return new PublicPerson(
                p.Id, p.PublicSlug!, DisplayName(p, th: true), DisplayName(p, th: false), p.Category, S("positionText", p.PositionText), S("photo", p.PhotoFileId),
                S("workEmail", p.WorkEmail), S("workPhone", p.WorkPhone), S("officeRoom", p.OfficeRoom), S("lineId", p.LineId),
                S("expertise", p.ExpertiseTh), S("expertise", p.ExpertiseEn), S("bio", p.BioTh), S("bio", p.BioEn),
                S("orcid", p.OrcidId), S("scopus", p.ScopusAuthorId), S("scholar", p.GoogleScholarId), S("researchgate", p.ResearchGateUrl), S("website", p.WebsiteUrl),
                shown, (pos?.Rank ?? 1000) * 1000 + p.DisplayOrder, pos?.NameTh, pos?.NameEn);
        }).OrderBy(p => p.Order).ToList();
    }

    /// <summary>Academic rank is part of a name in Thai usage ("ผศ.ดร."), so it is always shown.</summary>
    public static string DisplayName(Person p, bool th)
    {
        var rank = p.AcademicRank switch
        {
            AcademicRank.AsstProf => th ? "ผศ." : "Asst. Prof. ",
            AcademicRank.AssocProf => th ? "รศ." : "Assoc. Prof. ",
            AcademicRank.Prof => th ? "ศ." : "Prof. ",
            _ => "",
        };
        var dr = p.HasDoctorate ? (th ? "ดร." : "Dr. ") : "";
        var title = rank + dr;
        if (title.Length == 0) title = th ? (p.TitleTh ?? "") : (p.TitleEn != null ? p.TitleEn + " " : "");
        return th ? $"{title}{p.FirstNameTh} {p.LastNameTh}".Trim()
                  : $"{title}{p.FirstNameEn ?? p.FirstNameTh} {p.LastNameEn ?? p.LastNameTh}".Trim();
    }

    public async Task<(List<PersonEducation> Education, List<PersonEmployment> Work, List<Award> Awards, List<Publication> Pubs, List<Project> Projects)>
        PersonDetailsAsync(PublicPerson p)
    {
        var edu = p.Shown.Contains("education")
            ? await db.Set<PersonEducation>().AsNoTracking().Where(e => e.PersonId == p.Id && e.IsPublic && e.RecordStatus == Active).OrderByDescending(e => e.GraduatedYear).ToListAsync() : [];
        var work = p.Shown.Contains("employment")
            ? await db.Set<PersonEmployment>().AsNoTracking().Where(e => e.PersonId == p.Id && e.IsPublic && e.RecordStatus == Active).OrderByDescending(e => e.StartDate).ToListAsync() : [];
        // Rows linked to an organisation carry no text of their own; show the organisation's name.
        var orgIds = work.Where(w => w.OrganizationId != null && w.OrganizationText == null).Select(w => w.OrganizationId!).ToList();
        var orgNames = await db.Set<Organization>().AsNoTracking().Where(o => orgIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.NameEn ?? o.NameTh);
        foreach (var w in work.Where(w => w.OrganizationId != null && w.OrganizationText == null)) w.OrganizationText = orgNames.GetValueOrDefault(w.OrganizationId!);
        var awards = p.Shown.Contains("awards")
            ? await db.Set<AwardRecipient>().AsNoTracking().Where(a => a.PersonId == p.Id && a.RecordStatus == Active)
                .Join(db.Set<Award>().Where(a => a.IsPublic && a.RecordStatus == Active), r => r.AwardId, a => a.Id, (r, a) => a).OrderByDescending(a => a.Date).ToListAsync() : [];
        var pubs = p.Shown.Contains("publications")
            ? await db.Set<PublicationAuthor>().AsNoTracking().Where(a => a.PersonId == p.Id && a.RecordStatus == Active)
                .Join(PublishedPubs(), a => a.PublicationId, x => x.Id, (a, x) => x).OrderByDescending(x => x.Year).ToListAsync() : [];
        var projects = p.Shown.Contains("projects")
            ? await db.Set<ProjectMember>().AsNoTracking().Where(m => m.PersonId == p.Id && m.IsPublic && m.RecordStatus == Active)
                .Join(PublicProjects(), m => m.ProjectId, x => x.Id, (m, x) => x).Distinct().ToListAsync() : [];
        return (edu, work, awards, pubs, projects);
    }

    private IQueryable<Publication> PublishedPubs() =>
        db.Set<Publication>().AsNoTracking().Where(p => p.IsPublic && p.RecordStatus == Active && p.Stage == PublicationStage.Published);

    private IQueryable<Project> PublicProjects() =>
        db.Set<Project>().AsNoTracking().Where(p => p.IsPublic && p.RecordStatus == Active);

    public async Task<List<(Publication Pub, List<string> Authors)>> PublicationsAsync(int? year = null, PublicationType? type = null)
    {
        var q = PublishedPubs();
        if (year != null) q = q.Where(p => p.Year == year);
        if (type != null) q = q.Where(p => p.Type == type);
        var pubs = await q.OrderByDescending(p => p.Year).ThenBy(p => p.TitleEn).ToListAsync();
        var ids = pubs.Select(p => p.Id).ToList();
        var authors = await db.Set<PublicationAuthor>().AsNoTracking().Where(a => ids.Contains(a.PublicationId) && a.RecordStatus == Active)
            .OrderBy(a => a.Order).ToListAsync();
        var personIds = authors.Where(a => a.PersonId != null).Select(a => a.PersonId!).Distinct().ToList();
        var names = await db.Set<Person>().AsNoTracking().Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => (p.FirstNameEn != null ? p.FirstNameEn[0] + ". " : "") + (p.LastNameEn ?? p.LastNameTh));
        return pubs.Select(p => (p, authors.Where(a => a.PublicationId == p.Id)
            .Select(a => a.PersonId != null && names.TryGetValue(a.PersonId, out var n) ? n : a.ExternalName ?? "").ToList())).ToList();
    }

    public async Task<List<int>> PublicationYearsAsync() =>
        await PublishedPubs().Where(p => p.Year != null).Select(p => p.Year!.Value).Distinct().OrderByDescending(y => y).ToListAsync();

    public Task<List<Project>> ProjectsAsync() =>
        PublicProjects().OrderByDescending(p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Extended).ThenByDescending(p => p.StartDate).ToListAsync();

    public async Task<(Project Project, List<(string Name, ProjectRole Role, string? Slug)> Team, List<Organization> Partners, List<Publication> Pubs, List<ProjectOutput> Outputs)?> ProjectAsync(string id)
    {
        var p = await PublicProjects().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return null;
        var members = await db.Set<ProjectMember>().AsNoTracking().Where(m => m.ProjectId == id && m.IsPublic && m.RecordStatus == Active).ToListAsync();
        var mids = members.Select(m => m.PersonId).ToList();
        var people = await db.Set<Person>().AsNoTracking().Where(x => mids.Contains(x.Id)).ToListAsync();
        var team = members.OrderBy(m => m.Role).Select(m =>
        {
            var person = people.First(x => x.Id == m.PersonId);
            return (DisplayName(person, th: true) + "|" + DisplayName(person, th: false), m.Role, person.Kind == PersonKind.Internal ? person.PublicSlug : null);
        }).ToList();
        var partners = await db.Set<ProjectPartner>().AsNoTracking().Where(x => x.ProjectId == id && x.RecordStatus == Active)
            .Join(db.Set<Organization>(), x => x.OrganizationId, o => o.Id, (x, o) => o).ToListAsync();
        if (p.FunderOrganizationId != null && partners.All(o => o.Id != p.FunderOrganizationId))
        {
            var funder = await db.Set<Organization>().AsNoTracking().FirstOrDefaultAsync(o => o.Id == p.FunderOrganizationId);
            if (funder != null) partners.Insert(0, funder);
        }
        var pubs = await db.Set<PublicationProject>().AsNoTracking().Where(x => x.ProjectId == id && x.RecordStatus == Active)
            .Join(PublishedPubs(), x => x.PublicationId, pb => pb.Id, (x, pb) => pb).ToListAsync();
        var outputs = await db.Set<ProjectOutput>().AsNoTracking().Where(o => o.ProjectId == id && o.RecordStatus == Active).ToListAsync();
        return (p, team, partners, pubs, outputs);
    }

    public sealed record LineageNode(PublicPerson Person, MentorRole? Role, EducationLevel? Degree, List<LineageNode> Students);

    /// <summary>The group's academic family tree: roots are members with no advisor inside the group;
    /// a student with two advisors appears under the main one (co-advisors are noted on the profile).</summary>
    public async Task<List<LineageNode>> LineageAsync(List<PublicPerson> people)
    {
        var shown = people.Where(p => p.Shown.Contains("mentorship")).ToDictionary(p => p.Id);
        var links = await db.Set<PersonMentorship>().AsNoTracking()
            .Where(m => m.RecordStatus == Active && m.IsPublic && m.Role != MentorRole.CoAdvisor).ToListAsync();
        links = links.Where(m => shown.ContainsKey(m.StudentPersonId) && shown.ContainsKey(m.MentorPersonId)).ToList();
        var hasMentor = links.Select(l => l.StudentPersonId).ToHashSet();
        var mentors = links.Select(l => l.MentorPersonId).ToHashSet();
        LineageNode Build(string id, PersonMentorship? via, HashSet<string> path) => new(shown[id], via?.Role, via?.Degree,
            links.Where(l => l.MentorPersonId == id && !path.Contains(l.StudentPersonId))
                .Select(l => Build(l.StudentPersonId, l, [.. path, l.StudentPersonId])).OrderBy(n => n.Person.Order).ToList());
        return shown.Keys.Where(id => !hasMentor.Contains(id) && mentors.Contains(id))
            .Select(id => Build(id, null, [id])).OrderBy(n => n.Person.Order).ToList();
    }

    public Task<List<NewsPost>> NewsAsync(int take = 50, NewsKind? kind = null) =>
        db.Set<NewsPost>().AsNoTracking().Where(n => n.Status == PublishStatus.Published && n.RecordStatus == Active && (kind == null || n.Kind == kind))
            .OrderByDescending(n => n.Pinned).ThenByDescending(n => n.PublishedAt).Take(take).ToListAsync();

    public Task<NewsPost?> NewsPostAsync(string slug) =>
        db.Set<NewsPost>().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug && n.Status == PublishStatus.Published && n.RecordStatus == Active);

    public async Task<List<Recruitment>> OpenPositionsAsync()
    {
        var today = Clock.TodayBangkok();
        return await db.Set<Recruitment>().AsNoTracking()
            .Where(r => r.IsPublic && r.Status == RecruitmentStatus.Open && r.RecordStatus == Active && (r.CloseDate == null || r.CloseDate >= today) && (r.OpenDate == null || r.OpenDate <= today))
            .OrderBy(r => r.CloseDate).ToListAsync();
    }

    public Task<List<Asset>> FacilitiesAsync() =>
        db.Set<Asset>().AsNoTracking().Where(a => a.IsPublic && a.RecordStatus == Active && a.Status != AssetStatus.Disposed && a.Status != AssetStatus.Lost)
            .OrderBy(a => a.NameTh).ToListAsync();

    public Task<List<Organization>> PartnersAsync() =>
        db.Set<Organization>().AsNoTracking().Where(o => o.IsPublic && o.RecordStatus == Active).OrderBy(o => o.NameEn ?? o.NameTh).ToListAsync();

    public async Task<(int People, int Publications, int ActiveProjects, int Partners)> StatsAsync()
    {
        var today = Clock.TodayBangkok();
        return (
            await db.Set<Person>().CountAsync(p => p.RecordStatus == Active && p.Kind == PersonKind.Internal && p.PublicSlug != null && (p.LeftDate == null || p.LeftDate >= today)),
            await PublishedPubs().CountAsync(),
            await PublicProjects().CountAsync(p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Extended || p.Status == ProjectStatus.Funded),
            await db.Set<Organization>().CountAsync(o => o.IsPublic && o.RecordStatus == Active));
    }

    /// <summary>Files a public page may link to: images marked public, and nothing else.</summary>
    public Task<bool> IsPublicFileAsync(string id) => db.Set<FileBlob>().AnyAsync(f => f.Id == id && f.IsPublic && f.RecordStatus == Active);
}
