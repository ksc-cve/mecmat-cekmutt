using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;
using Mecmat.Site;

namespace Mecmat.Pages;

public sealed class IndexModel(PublicQueryService q) : SitePageModel(q)
{
    public List<ResearchArea> Areas { get; private set; } = [];
    public List<NewsPost> News { get; private set; } = [];
    public List<Recruitment> Positions { get; private set; } = [];
    public List<(Publication Pub, List<string> Authors)> Pubs { get; private set; } = [];
    public (int People, int Publications, int ActiveProjects, int Partners) Stats { get; private set; }
    public SitePage? About { get; private set; }

    public async Task OnGetAsync()
    {
        Areas = await Q.AreasAsync();
        News = await Q.NewsAsync(3);
        Positions = await Q.OpenPositionsAsync();
        Pubs = (await Q.PublicationsAsync()).Take(5).ToList();
        Stats = await Q.StatsAsync();
        About = await Q.PageAsync("about");
    }
}

public sealed class SimplePageModel(PublicQueryService q) : SitePageModel(q)
{
    public SitePage? PageContent { get; private set; }

    public async Task<IActionResult> OnGetAsync(string key = "privacy")
    {
        PageContent = await Q.PageAsync(key);
        if (PageContent == null) return NotFound();
        PageTitle = Pick(PageContent.TitleTh, PageContent.TitleEn);
        return Page();
    }
}

public sealed class AboutModel(PublicQueryService q) : SitePageModel(q)
{
    public SitePage? PageContent { get; private set; }
    public List<PublicPerson> Leaders { get; private set; } = [];
    public List<ResearchArea> Areas { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PageContent = await Q.PageAsync("about");
        Leaders = (await Q.PeopleAsync()).Where(p => p.GroupPositionTh != null).ToList();
        Areas = await Q.AreasAsync();
        PageTitle = L("nav.about");
    }
}

public sealed class ResearchModel(PublicQueryService q) : SitePageModel(q)
{
    public List<ResearchArea> Areas { get; private set; } = [];
    public List<Project> Projects { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Areas = await Q.AreasAsync();
        Projects = (await Q.ProjectsAsync()).Take(6).ToList();
        PageTitle = L("nav.research");
    }
}

public sealed class PeopleModel(PublicQueryService q) : SitePageModel(q)
{
    public List<PublicPerson> People { get; private set; } = [];

    public async Task OnGetAsync()
    {
        People = await Q.PeopleAsync();
        PageTitle = L("nav.people");
    }
}

public sealed class PersonModel(PublicQueryService q, TimelineService timeline) : SitePageModel(q)
{
    public CareerView Career { get; private set; } = null!;
    public PublicPerson Person { get; private set; } = null!;
    public List<PersonEducation> Education { get; private set; } = [];
    public List<PersonEmployment> Work { get; private set; } = [];
    public List<Award> Awards { get; private set; } = [];
    public List<Publication> Pubs { get; private set; } = [];
    public List<Project> Projects { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var p = await Q.PersonAsync(slug);
        if (p == null) return NotFound();
        Person = p;
        (Education, Work, Awards, Pubs, Projects) = await Q.PersonDetailsAsync(p);
        Career = await timeline.BuildAsync(p.Id, publicOnly: true, p.Shown);
        PageTitle = Pick(p.NameTh, p.NameEn);
        MetaDescription = Pick(p.ExpertiseTh, p.ExpertiseEn);
        OgImage = p.PhotoFileId == null ? null : Img(p.PhotoFileId);
        return Page();
    }
}

public sealed class ProjectsModel(PublicQueryService q) : SitePageModel(q)
{
    public List<Project> Projects { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Projects = await Q.ProjectsAsync();
        PageTitle = L("nav.projects");
    }
}

public sealed class ProjectModel(PublicQueryService q) : SitePageModel(q)
{
    public Project Project { get; private set; } = null!;
    public List<(string Name, ProjectRole Role, string? Slug)> Team { get; private set; } = [];
    public List<Organization> Partners { get; private set; } = [];
    public List<Publication> Pubs { get; private set; } = [];
    public List<ProjectOutput> Outputs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var r = await Q.ProjectAsync(id);
        if (r == null) return NotFound();
        (Project, Team, Partners, Pubs, Outputs) = r.Value;
        PageTitle = Pick(Project.TitleTh, Project.TitleEn);
        MetaDescription = Pick(Project.PublicSummaryTh, Project.PublicSummaryEn);
        OgImage = Project.CoverFileId == null ? null : Img(Project.CoverFileId);
        return Page();
    }
}

public sealed class PublicationsModel(PublicQueryService q) : SitePageModel(q)
{
    public List<(Publication Pub, List<string> Authors)> Pubs { get; private set; } = [];
    public List<int> Years { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int? YearFilter { get; set; }
    [BindProperty(SupportsGet = true)] public PublicationType? TypeFilter { get; set; }

    public async Task OnGetAsync()
    {
        Years = await Q.PublicationYearsAsync();
        Pubs = await Q.PublicationsAsync(YearFilter, TypeFilter);
        PageTitle = L("nav.publications");
    }
}

public sealed class NewsModel(PublicQueryService q) : SitePageModel(q)
{
    public List<NewsPost> Posts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Posts = await Q.NewsAsync(100);
        PageTitle = L("nav.news");
    }
}

public sealed class NewsPostModel(PublicQueryService q) : SitePageModel(q)
{
    public NewsPost Post { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var p = await Q.NewsPostAsync(slug);
        if (p == null) return NotFound();
        Post = p;
        PageTitle = Pick(p.TitleTh, p.TitleEn);
        MetaDescription = Pick(p.SummaryTh, p.SummaryEn);
        OgImage = p.CoverFileId == null ? null : Img(p.CoverFileId);
        return Page();
    }
}

public sealed class FacilitiesModel(PublicQueryService q) : SitePageModel(q)
{
    public List<Asset> Assets { get; private set; } = [];
    public async Task OnGetAsync() { Assets = await Q.FacilitiesAsync(); PageTitle = L("nav.facilities"); }
}

public sealed class PartnersModel(PublicQueryService q) : SitePageModel(q)
{
    public List<Organization> Orgs { get; private set; } = [];
    public async Task OnGetAsync() { Orgs = await Q.PartnersAsync(); PageTitle = L("nav.partners"); }
}

/// <summary>Shared rules for the two public forms: honeypot, per-IP rate limit, consent record.
/// There is no e-mail, so what arrives lands in the back-office inbox with an in-app alert.</summary>
public abstract class FormPageModel(PublicQueryService q, AppDbContext db, NotificationService notify, LoginThrottle throttle, AppOptions opt) : SitePageModel(q)
{
    protected AppDbContext Db { get; } = db;
    protected NotificationService Notify { get; } = notify;
    public string? Error { get; set; }
    public bool Sent { get; set; }
    [BindProperty] public string? Website { get; set; } // honeypot: people never see or fill it

    protected string IpHash => Mecmat.Auth.Tokens.HashIp(HttpContext.Connection.RemoteIpAddress?.ToString(), opt.IpSalt);

    protected bool Throttled(string kind)
    {
        var key = $"{kind}:{IpHash}";
        if (throttle.Blocked(key)) return true;
        throttle.Fail(key);
        return false;
    }

    protected static bool ValidEmail(string? e)
    {
        if (string.IsNullOrWhiteSpace(e) || e.Length > 200) return false;
        try { return new MailAddress(e).Address == e.Trim(); } catch (FormatException) { return false; }
    }

    protected async Task<ConsentRecord> ConsentAsync(string email, string purpose)
    {
        var c = new ConsentRecord
        {
            SubjectEmail = email, Purpose = purpose, PolicyVersion = S("privacy.version"), GivenAt = DateTime.UtcNow, IpHash = IpHash,
        };
        Db.Add(c);
        await Db.SaveChangesAsync();
        return c;
    }

    protected async Task NotifyInboxAsync(string title, object args, string link)
    {
        var roleIds = await Db.Set<RolePermission>().AsNoTracking().Where(p => p.Module == Mecmat.Auth.Modules.Inbox && p.AllAction >= PermAction.View).Select(p => p.RoleId).ToListAsync();
        var users = await Db.Set<UserRole>().AsNoTracking().Where(u => roleIds.Contains(u.RoleId) && u.RecordStatus == RecordStatus.Active).Select(u => u.UserId).ToListAsync();
        users.AddRange(await Db.Set<UserAccount>().AsNoTracking().Where(u => u.IsAdmin && u.Status == AccountStatus.Active).Select(u => u.Id).ToListAsync());
        await Notify.NotifyUsersAsync(users, "inbox.new", title, System.Text.Json.JsonSerializer.Serialize(args, Json.Options), link, groupKey: title);
    }
}

public sealed class ContactModel(PublicQueryService q, AppDbContext db, NotificationService notify, LoginThrottle throttle, AppOptions opt)
    : FormPageModel(q, db, notify, throttle, opt)
{
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Subject { get; set; }
    [BindProperty] public string? Message { get; set; }
    [BindProperty] public bool Consent { get; set; }
    public SitePage? PageContent { get; private set; }

    public async Task OnGetAsync() { PageContent = await Q.PageAsync("contact"); PageTitle = L("nav.contact"); }

    public async Task<IActionResult> OnPostAsync()
    {
        PageContent = await Q.PageAsync("contact");
        PageTitle = L("nav.contact");
        if (!string.IsNullOrEmpty(Website)) { Sent = true; return Page(); } // bots get a polite nothing
        if (string.IsNullOrWhiteSpace(Name) || !ValidEmail(Email) || string.IsNullOrWhiteSpace(Subject) || string.IsNullOrWhiteSpace(Message)
            || Name.Length > 200 || Subject.Length > 300 || Message.Length > 5000) { Error = L("form.required"); return Page(); }
        if (!Consent) { Error = L("form.consentRequired"); return Page(); }
        if (Throttled("contact")) { Error = L("form.tooMany"); return Page(); }
        var consent = await ConsentAsync(Email!.Trim(), "ContactForm");
        Db.Add(new ContactMessage
        {
            Name = Name.Trim(), Email = Email.Trim(), Phone = Phone?.Trim(), Subject = Subject.Trim(), Message = Message.Trim(),
            ConsentRecordId = consent.Id, SubmittedAt = DateTime.UtcNow, IpHash = IpHash,
        });
        await NotifyInboxAsync("notify.inbox.message", new { name = Name.Trim(), subject = Subject.Trim() }, "#/inbox");
        await Db.SaveChangesAsync();
        Sent = true;
        return Page();
    }
}

[RequestSizeLimit(11_000_000)]
public sealed class JoinModel(PublicQueryService q, AppDbContext db, NotificationService notify, LoginThrottle throttle, AppOptions opt, FileStorage files)
    : FormPageModel(q, db, notify, throttle, opt)
{
    public List<Recruitment> Positions { get; private set; } = [];
    public SitePage? PageContent { get; private set; }
    [BindProperty] public string? RecruitmentId { get; set; }
    [BindProperty] public string? FullName { get; set; }
    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Message { get; set; }
    [BindProperty] public bool Consent { get; set; }
    [BindProperty] public IFormFile? Cv { get; set; }

    private async Task LoadAsync()
    {
        Positions = await Q.OpenPositionsAsync();
        PageContent = await Q.PageAsync("join-us");
        PageTitle = L("nav.join");
    }

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!string.IsNullOrEmpty(Website)) { Sent = true; return Page(); }
        var position = Positions.FirstOrDefault(p => p.Id == RecruitmentId);
        if (position == null || string.IsNullOrWhiteSpace(FullName) || !ValidEmail(Email) || FullName.Length > 200 || (Message?.Length ?? 0) > 5000)
        { Error = L("form.required"); return Page(); }
        if (!Consent) { Error = L("form.consentRequired"); return Page(); }
        var ext = Path.GetExtension(Cv?.FileName ?? "").ToLowerInvariant();
        if (Cv != null && (Cv.Length > 10_000_000 || ext is not (".pdf" or ".docx"))) { Error = L("form.badFile"); return Page(); }
        if (Throttled("apply")) { Error = L("form.tooMany"); return Page(); }
        var consent = await ConsentAsync(Email!.Trim(), "Application");
        var cv = Cv == null ? null : await files.SaveAsync(Cv, isPublic: false);
        Db.Add(new Application
        {
            RecruitmentId = position.Id, FullName = FullName.Trim(), Email = Email.Trim(), Phone = Phone?.Trim(), Message = Message?.Trim(),
            CvFileId = cv?.Id, ConsentRecordId = consent.Id, SubmittedAt = DateTime.UtcNow, IpHash = IpHash,
        });
        await NotifyInboxAsync("notify.inbox.application", new { name = FullName.Trim(), position = position.TitleTh }, "#/inbox?tab=applications");
        await Db.SaveChangesAsync();
        Sent = true;
        return Page();
    }
}

public sealed class ErrorModel(PublicQueryService q) : SitePageModel(q)
{
    public int Code { get; private set; }
    public void OnGet(int code) { Code = code; PageTitle = code == 404 ? L("error.404") : L("error.other"); }
}
