using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mecmat.Domain;

namespace Mecmat.Data;

/// <summary>First-run import of the group's people and their public record from
/// Data/SeedData/group.json, compiled from public sources (staff pages, ORCID, Scholar, news).
/// Keeping it as data rather than C# lets the owner correct a fact without touching code; after the
/// first run everything is edited in the app.</summary>
public static class GroupSeed
{
    public sealed class Root
    {
        public List<OrgJ> Organizations { get; set; } = [];
        public List<PersonJ> People { get; set; } = [];
        public List<MentorJ> Mentorships { get; set; } = [];
        public List<PubJ> Publications { get; set; } = [];
    }
    public sealed class OrgJ { public string Key { get; set; } = ""; public string NameTh { get; set; } = ""; public string? NameEn { get; set; } public string? ShortName { get; set; } public OrganizationKind Kind { get; set; } = OrganizationKind.Other; public string? Country { get; set; } public string? Website { get; set; } public string? Parent { get; set; } public bool IsPublic { get; set; } }
    public sealed class PersonJ
    {
        public string Key { get; set; } = ""; public PersonKind Kind { get; set; } = PersonKind.Internal; public PersonCategory Category { get; set; } = PersonCategory.Faculty;
        public string? TitleTh { get; set; } public string? TitleEn { get; set; } public AcademicRank Rank { get; set; } public bool Dr { get; set; }
        public string FirstNameTh { get; set; } = ""; public string LastNameTh { get; set; } = ""; public string? FirstNameEn { get; set; } public string? LastNameEn { get; set; }
        public string? PositionText { get; set; } public string? WorkEmail { get; set; } public string? WorkPhone { get; set; } public string? Org { get; set; }
        public string? ExpertiseTh { get; set; } public string? ExpertiseEn { get; set; } public string? BioTh { get; set; } public string? BioEn { get; set; }
        public string? Orcid { get; set; } public string? Scopus { get; set; } public string? Scholar { get; set; } public string? ResearchGate { get; set; } public string? Website { get; set; }
        public string? Slug { get; set; } public int Order { get; set; } = 100; public DateOnly? Joined { get; set; } public DateOnly? Left { get; set; }
        public List<EduJ> Education { get; set; } = []; public List<JobJ> Employment { get; set; } = []; public List<RankJ> Appointments { get; set; } = [];
        public List<AwardJ> Awards { get; set; } = []; public List<EventJ> Events { get; set; } = []; public List<PosJ> Positions { get; set; } = [];
        public List<string> Public { get; set; } = []; public List<LicJ> Licenses { get; set; } = [];
    }
    public sealed class LicJ { public string Body { get; set; } = ""; public string? Level { get; set; } public string? Branch { get; set; } public DateOnly? Issued { get; set; } }
    public sealed class EduJ { public EducationLevel Level { get; set; } public string? DegreeTh { get; set; } public string? DegreeEn { get; set; } public string? Field { get; set; } public string? Institution { get; set; } public string? Country { get; set; } public int? StartYear { get; set; } public int? Year { get; set; } public string? Thesis { get; set; } public string? Honors { get; set; } }
    public sealed class JobJ { public string Position { get; set; } = ""; public string? Org { get; set; } public string? OrgText { get; set; } public DateOnly? From { get; set; } public DateOnly? To { get; set; } public string? Description { get; set; } }
    public sealed class RankJ { public AcademicRank Rank { get; set; } public DateOnly? Date { get; set; } public string? Field { get; set; } }
    public sealed class AwardJ { public string Title { get; set; } = ""; public string? TitleEn { get; set; } public string? Issuer { get; set; } public DateOnly? Date { get; set; } public AwardLevel Level { get; set; } = AwardLevel.Other; public string? Description { get; set; } public List<string> With { get; set; } = []; }
    public sealed class EventJ { public CareerEventKind Kind { get; set; } public DateOnly Date { get; set; } public DateOnly? End { get; set; } public bool Approx { get; set; } public string TitleTh { get; set; } = ""; public string? TitleEn { get; set; } public string? DescTh { get; set; } public string? DescEn { get; set; } public string? Org { get; set; } public string? OrgText { get; set; } public string? Country { get; set; } public string? Url { get; set; } public string? Source { get; set; } }
    public sealed class PosJ { public string Position { get; set; } = ""; public DateOnly? From { get; set; } public DateOnly? To { get; set; } }
    public sealed class MentorJ { public string Student { get; set; } = ""; public string Mentor { get; set; } = ""; public MentorRole Role { get; set; } = MentorRole.MainAdvisor; public EducationLevel? Degree { get; set; } public int? From { get; set; } public int? To { get; set; } public string? Thesis { get; set; } }
    public sealed class PubJ
    {
        public string Title { get; set; } = ""; public int? Year { get; set; } public string? Venue { get; set; } public string? Volume { get; set; } public string? Pages { get; set; }
        public string? Doi { get; set; } public PublicationType Type { get; set; } = PublicationType.JournalArticle; public List<string> Authors { get; set; } = [];
        public string? Index { get; set; } public string? Quartile { get; set; }
    }

    private static readonly JsonSerializerOptions Opts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true,
    };

    public static Root Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "group.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<Root>(File.ReadAllText(path), Opts) ?? new Root() : new Root();
    }

    /// <summary>Returns the person ids by seed key, so the caller can attach the admin account.</summary>
    public static async Task<Dictionary<string, string>> ImportAsync(AppDbContext db, Root data, Dictionary<string, string> positionsByName)
    {
        var orgs = new Dictionary<string, string>();
        foreach (var o in data.Organizations)
        {
            var row = new Organization { NameTh = o.NameTh, NameEn = o.NameEn, ShortName = o.ShortName, Kind = o.Kind, Country = o.Country, Website = o.Website, IsPublic = o.IsPublic,
                ParentOrganizationId = o.Parent != null ? orgs.GetValueOrDefault(o.Parent) : null };
            db.Add(row);
            orgs[o.Key] = row.Id;
        }
        await db.SaveChangesAsync();
        string? Org(string? key) => key == null ? null : orgs.GetValueOrDefault(key);

        var people = new Dictionary<string, string>();
        foreach (var p in data.People)
        {
            var row = new Person
            {
                Kind = p.Kind, Category = p.Category, TitleTh = p.TitleTh, TitleEn = p.TitleEn, AcademicRank = p.Rank, HasDoctorate = p.Dr,
                FirstNameTh = p.FirstNameTh, LastNameTh = p.LastNameTh, FirstNameEn = p.FirstNameEn, LastNameEn = p.LastNameEn,
                PositionText = p.PositionText, WorkEmail = p.WorkEmail, WorkPhone = p.WorkPhone, OrganizationId = Org(p.Org),
                ExpertiseTh = p.ExpertiseTh, ExpertiseEn = p.ExpertiseEn, BioTh = p.BioTh, BioEn = p.BioEn,
                OrcidId = p.Orcid, ScopusAuthorId = p.Scopus, GoogleScholarId = p.Scholar, ResearchGateUrl = p.ResearchGate, WebsiteUrl = p.Website,
                PublicSlug = p.Slug, DisplayOrder = p.Order, JoinedDate = p.Joined, LeftDate = p.Left,
            };
            db.Add(row);
            people[p.Key] = row.Id;
            foreach (var e in p.Education)
                db.Add(new PersonEducation { PersonId = row.Id, Level = e.Level, DegreeNameTh = e.DegreeTh, DegreeNameEn = e.DegreeEn, Field = e.Field, Institution = e.Institution, Country = e.Country, StartYear = e.StartYear, GraduatedYear = e.Year, ThesisTitle = e.Thesis, Honors = e.Honors });
            foreach (var j in p.Employment)
                db.Add(new PersonEmployment { PersonId = row.Id, Position = j.Position, OrganizationId = Org(j.Org), OrganizationText = j.OrgText, StartDate = j.From, EndDate = j.To, Description = j.Description });
            foreach (var a in p.Appointments)
                db.Add(new AcademicAppointment { PersonId = row.Id, Rank = a.Rank, AppointedDate = a.Date, FieldName = a.Field });
            foreach (var ev in p.Events)
                db.Add(new PersonEvent { PersonId = row.Id, Kind = ev.Kind, Date = ev.Date, EndDate = ev.End, DateIsApproximate = ev.Approx, TitleTh = ev.TitleTh, TitleEn = ev.TitleEn,
                    DescriptionTh = ev.DescTh, DescriptionEn = ev.DescEn, OrganizationId = Org(ev.Org), OrganizationText = ev.OrgText, Country = ev.Country, Url = ev.Url, SourceUrl = ev.Source });
            foreach (var pos in p.Positions)
                if (positionsByName.TryGetValue(pos.Position, out var posId))
                    db.Add(new PositionAssignment { PositionId = posId, PersonId = row.Id, FromDate = pos.From, ToDate = pos.To });
            foreach (var l in p.Licenses)
                db.Add(new ProfessionalLicense { PersonId = row.Id, Body = l.Body, Level = l.Level, Branch = l.Branch, IssuedDate = l.Issued, IsPublic = true });
            foreach (var key in p.Public.Distinct())
                db.Add(new PublicFieldVisibility { PersonId = row.Id, FieldKey = key, IsPublic = true });
        }
        await db.SaveChangesAsync();

        // Awards may be shared (a team prize); create each once and give it to everyone named.
        var awardsDone = new Dictionary<string, string>();
        foreach (var p in data.People)
            foreach (var a in p.Awards)
            {
                var k = a.Title + "|" + a.Date;
                if (!awardsDone.TryGetValue(k, out var awardId))
                {
                    var row = new Award { TitleTh = a.Title, TitleEn = a.TitleEn, Issuer = a.Issuer, Date = a.Date, Level = a.Level, Description = a.Description };
                    db.Add(row);
                    awardsDone[k] = awardId = row.Id;
                }
                foreach (var who in a.With.Append(p.Key).Distinct())
                    if (people.TryGetValue(who, out var pid) && !db.ChangeTracker.Entries<AwardRecipient>().Any(e => e.Entity.AwardId == awardId && e.Entity.PersonId == pid))
                        db.Add(new AwardRecipient { AwardId = awardId, PersonId = pid });
            }

        foreach (var m in data.Mentorships)
            if (people.TryGetValue(m.Student, out var s) && people.TryGetValue(m.Mentor, out var t))
                db.Add(new PersonMentorship { StudentPersonId = s, MentorPersonId = t, Role = m.Role, Degree = m.Degree, FromYear = m.From, ToYear = m.To, ThesisTitle = m.Thesis });
        await db.SaveChangesAsync();

        // Authors are written as seed keys ("@kasan") for members and plain names for everyone else.
        var seenDoi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pub in data.Publications)
        {
            if (pub.Doi != null && !seenDoi.Add(pub.Doi)) continue;
            var conf = pub.Type == PublicationType.ConferencePaper;
            var row = new Publication
            {
                Type = pub.Type, VenueKind = conf ? VenueKind.Conference : VenueKind.Journal, Stage = PublicationStage.Published,
                TitleEn = pub.Title, VenueName = pub.Venue, Year = pub.Year, Volume = pub.Volume, Pages = pub.Pages,
                Doi = pub.Doi, Url = pub.Doi == null ? null : "https://doi.org/" + pub.Doi,
            };
            db.Add(row);
            for (var i = 0; i < pub.Authors.Count; i++)
            {
                var a = pub.Authors[i];
                var member = a.StartsWith('@') && people.TryGetValue(a[1..], out var pid) ? pid : null;
                db.Add(new PublicationAuthor { PublicationId = row.Id, Order = i + 1, PersonId = member, ExternalName = member == null ? a.TrimStart('@') : null, IsFirstAuthor = i == 0 });
            }
            if (pub.Index != null && Enum.TryParse<IndexDatabase>(pub.Index, out var idx))
                db.Add(new PublicationIndexing { PublicationId = row.Id, Database = idx, Quartile = pub.Quartile });
        }
        await db.SaveChangesAsync();
        return people;
    }
}
