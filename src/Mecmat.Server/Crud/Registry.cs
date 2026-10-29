using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Domain;
using Mecmat.Services;
using M = Mecmat.Auth.Modules;

namespace Mecmat.Crud;

public static class Registry
{
    private static readonly PermAction Child = PermAction.Edit;

    /// <summary>Keys a person may expose on the public site (ADR 0007). Anything not listed —
    /// national ID, birth date, payout, mobile, personal e-mail — has no key and cannot be published.</summary>
    public static readonly string[] PublicFieldKeys =
    [
        "photo", "positionText", "academicRank", "workEmail", "workPhone", "officeRoom", "lineId", "expertise", "bio",
        "orcid", "scopus", "scholar", "researchgate", "website", "education", "employment", "licenses", "awards",
        "publications", "projects",
    ];

    public static readonly ResourceDef[] All =
    [
        // ── M1 people & organisations
        new ResourceDef<Person>
        {
            Key = "people", Module = M.People, SearchFields = ["FirstNameTh", "LastNameTh", "FirstNameEn", "LastNameEn", "Nickname", "WorkEmail"],
            OrderBy = nameof(Person.DisplayOrder), OrderDescending = false,
            Related = s => x => s.RelatedPersonIds.Contains(x.Id),
            BeforeSave = async (c, x, _) =>
            {
                if (x.PublicSlug != null)
                {
                    x.PublicSlug = Slug.Make(x.PublicSlug);
                    if (await c.Db.Set<Person>().AnyAsync(p => p.PublicSlug == x.PublicSlug && p.Id != x.Id))
                        throw new AppException("people.slugTaken");
                }
            },
        },
        new ResourceDef<PersonEducation> { Key = "person-education", Module = M.People, DeleteNeeds = Child, OrderBy = nameof(PersonEducation.GraduatedYear), Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        new ResourceDef<PersonEmployment> { Key = "person-employment", Module = M.People, DeleteNeeds = Child, OrderBy = nameof(PersonEmployment.StartDate), Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        new ResourceDef<AcademicAppointment> { Key = "academic-appointments", Module = M.People, DeleteNeeds = Child, OrderBy = nameof(AcademicAppointment.AppointedDate), Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        new ResourceDef<TeachingRecord> { Key = "teaching-records", Module = M.People, DeleteNeeds = Child, OrderBy = nameof(TeachingRecord.AcademicYear), Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        new ResourceDef<ProfessionalLicense> { Key = "licenses", Module = M.People, DeleteNeeds = Child, Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        // Payout details are one's own business only: "related" is the person themself, not advisees.
        new ResourceDef<PayoutChannel> { Key = "payout-channels", Module = M.Sensitive, DeleteNeeds = Child, Related = s => x => x.PersonId == s.PersonId },
        new ResourceDef<PublicFieldVisibility>
        {
            Key = "public-visibility", Module = M.People, DeleteNeeds = Child, Related = s => x => x.PersonId == s.PersonId,
            BeforeSave = async (c, x, _) =>
            {
                if (!PublicFieldKeys.Contains(x.FieldKey)) throw new AppException("people.fieldNotPublishable");
                if (await c.Db.Set<PublicFieldVisibility>().AnyAsync(v => v.PersonId == x.PersonId && v.FieldKey == x.FieldKey && v.Id != x.Id))
                    throw new AppException("errors.duplicate");
            },
        },
        new ResourceDef<Organization> { Key = "organizations", Module = M.Organizations, SearchFields = ["NameTh", "NameEn", "ShortName"], OrderBy = nameof(Organization.NameTh), OrderDescending = false, Related = s => x => x.CreatedBy == s.UserId },
        new ResourceDef<Award> { Key = "awards", Module = M.People, OrderBy = nameof(Award.Date), Related = s => x => x.CreatedBy == s.UserId },
        new ResourceDef<AwardRecipient> { Key = "award-recipients", Module = M.People, DeleteNeeds = Child, Related = s => x => s.RelatedPersonIds.Contains(x.PersonId) },
        new ResourceDef<ResearchArea> { Key = "research-areas", Module = M.Cms, OrderBy = nameof(ResearchArea.DisplayOrder), OrderDescending = false },
        new ResourceDef<ResearchAreaLink>
        {
            Key = "research-area-links", Module = M.People, DeleteNeeds = Child,
            Related = s => x => (x.TargetType == "Person" && s.RelatedPersonIds.Contains(x.TargetId))
                                || (x.TargetType == "Project" && s.LeadProjectIds.Contains(x.TargetId))
                                || (x.TargetType == "Publication" && s.PublicationIds.Contains(x.TargetId)),
        },

        // ── M2 authority structure (users themselves go through UsersController)
        new ResourceDef<Role> { Key = "roles", Module = M.Users, OrderBy = nameof(Role.Key), OrderDescending = false, ServerFields = [nameof(Role.IsSystem)] },
        new ResourceDef<RolePermission> { Key = "role-permissions", Module = M.Users, DeleteNeeds = Child },
        new ResourceDef<UserPermissionOverride> { Key = "permission-overrides", Module = M.Users, DeleteNeeds = Child },
        new ResourceDef<GroupPosition> { Key = "positions", Module = M.ApprovalsConfig, OrderBy = nameof(GroupPosition.Rank), OrderDescending = false },
        new ResourceDef<PositionAssignment> { Key = "position-assignments", Module = M.ApprovalsConfig, DeleteNeeds = Child },
        new ResourceDef<ApprovalPolicy> { Key = "approval-policies", Module = M.ApprovalsConfig, OrderBy = nameof(ApprovalPolicy.Priority), OrderDescending = false },
        new ResourceDef<ApprovalPolicyStep> { Key = "approval-policy-steps", Module = M.ApprovalsConfig, DeleteNeeds = Child, OrderBy = nameof(ApprovalPolicyStep.StepOrder), OrderDescending = false },
        new ResourceDef<Delegation> { Key = "delegations", Module = M.ApprovalsConfig, DeleteNeeds = Child, Related = s => x => x.FromPersonId == s.PersonId },

        // ── M3 publications
        new ResourceDef<Publication>
        {
            Key = "publications", Module = M.Publications, SearchFields = ["TitleEn", "TitleTh", "VenueName", "Doi", "Keywords"],
            OrderBy = nameof(Publication.Year), Related = s => x => s.PublicationIds.Contains(x.Id) || x.CreatedBy == s.UserId,
            BeforeSave = (_, x, _) =>
            {
                if (x.Doi != null) x.Doi = Doi.Normalise(x.Doi);
                return Task.CompletedTask;
            },
        },
        new ResourceDef<PublicationAuthor>
        {
            Key = "publication-authors", Module = M.Publications, DeleteNeeds = Child, OrderBy = nameof(PublicationAuthor.Order), OrderDescending = false,
            Related = s => x => s.PublicationIds.Contains(x.PublicationId),
            // The signed ก.พ.อ. contribution form cannot be amended, so neither can a certified row.
            BeforeSave = (c, x, before) => before?.CertifiedAt != null && !c.User.CanAll(M.Publications, PermAction.Manage)
                ? throw new AppException("publications.authorCertified") : Task.CompletedTask,
        },
        new ResourceDef<PublicationIndexing> { Key = "publication-indexing", Module = M.Publications, DeleteNeeds = Child, Related = s => x => s.PublicationIds.Contains(x.PublicationId) },
        new ResourceDef<PublicationUsage> { Key = "publication-usage", Module = M.Publications, DeleteNeeds = Child, Related = s => x => s.PublicationIds.Contains(x.PublicationId) },
        new ResourceDef<PublicationProject> { Key = "publication-projects", Module = M.Publications, DeleteNeeds = Child, Related = s => x => s.PublicationIds.Contains(x.PublicationId) || s.LeadProjectIds.Contains(x.ProjectId) },

        // ── M4 projects
        new ResourceDef<Project>
        {
            Key = "projects", Module = M.Projects, SearchFields = ["TitleTh", "TitleEn", "Code", "ExternalCode", "ContractNo"],
            OrderBy = nameof(Project.StartDate), Related = s => x => s.ProjectIds.Contains(x.Id) || x.CreatedBy == s.UserId,
            AfterCreate = async (c, x) =>
            {
                // Whoever opens a project leads it until they hand it over; without this the creator
                // could not reach the project they just made.
                if (c.User.PersonId != null)
                    c.Db.Add(new ProjectMember { ProjectId = x.Id, PersonId = c.User.PersonId, Role = ProjectRole.PI });
                await c.Db.SaveChangesAsync();
            },
        },
        new ResourceDef<ProjectMember> { Key = "project-members", Module = M.Projects, DeleteNeeds = Child, Related = s => x => s.LeadProjectIds.Contains(x.ProjectId) },
        new ResourceDef<ProjectPartner> { Key = "project-partners", Module = M.Projects, DeleteNeeds = Child, Related = s => x => s.LeadProjectIds.Contains(x.ProjectId) },
        new ResourceDef<Activity> { Key = "activities", Module = M.Projects, DeleteNeeds = Child, OrderBy = nameof(Activity.Order), OrderDescending = false, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<Milestone> { Key = "milestones", Module = M.Projects, DeleteNeeds = Child, OrderBy = nameof(Milestone.DueDate), OrderDescending = false, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<ProjectOutput> { Key = "project-outputs", Module = M.Projects, DeleteNeeds = Child, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<ProjectOutcome> { Key = "project-outcomes", Module = M.Projects, DeleteNeeds = Child, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<ProjectImpact> { Key = "project-impacts", Module = M.Projects, DeleteNeeds = Child, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<KeyResult> { Key = "key-results", Module = M.Projects, DeleteNeeds = Child, Related = s => x => x.ProjectId != null && s.LeadProjectIds.Contains(x.ProjectId) },
        new ResourceDef<ProgressReport> { Key = "progress-reports", Module = M.Projects, DeleteNeeds = Child, OrderBy = nameof(ProgressReport.DueDate), OrderDescending = false, Related = s => x => s.ProjectIds.Contains(x.ProjectId) },
        new ResourceDef<Recruitment> { Key = "recruitments", Module = M.Projects, Related = s => x => (x.ProjectId != null && s.LeadProjectIds.Contains(x.ProjectId)) || x.CreatedBy == s.UserId },
        new ResourceDef<Application>
        {
            Key = "applications", Module = M.Inbox, OrderBy = nameof(Application.SubmittedAt),
            ServerFields = [nameof(Application.FullName), nameof(Application.Email), nameof(Application.Phone), nameof(Application.Message),
                nameof(Application.CvFileId), nameof(Application.ConsentRecordId), nameof(Application.SubmittedAt), nameof(Application.IpHash), nameof(Application.RecruitmentId)],
            BeforeSave = (_, _, before) => before == null ? throw new AppException("applications.publicFormOnly") : Task.CompletedTask,
        },

        // ── M5 finance (money moves only through FinanceController)
        new ResourceDef<Account>
        {
            Key = "accounts", Module = M.Finance, OrderBy = nameof(Account.Code), OrderDescending = false, SearchFields = ["Code", "NameTh", "NameEn"],
            Related = s => x => s.AccountIds.Contains(x.Id) || (x.ParentAccountId != null && s.AccountIds.Contains(x.ParentAccountId)),
            BeforeSave = Finance.FinanceRules.BeforeAccountSaveAsync,
        },
        new ResourceDef<AccountBudget> { Key = "account-budgets", Module = M.Finance, DeleteNeeds = Child, Related = s => x => s.AccountIds.Contains(x.AccountId) },
        new ResourceDef<LedgerEntry> { Key = "ledger", Module = M.Finance, ReadOnly = true, OrderBy = nameof(LedgerEntry.EntryDate), Related = s => x => (x.FromAccountId != null && s.AccountIds.Contains(x.FromAccountId)) || (x.ToAccountId != null && s.AccountIds.Contains(x.ToAccountId)) },

        // ── M6 assets
        new ResourceDef<Asset> { Key = "assets", Module = M.Assets, SearchFields = ["NameTh", "NameEn", "AssetCode", "InternalCode", "SerialNo", "Brand", "Model"], OrderBy = nameof(Asset.AssetCode), OrderDescending = false, Related = s => x => x.CustodianPersonId == s.PersonId },
        new ResourceDef<AssetLoan> { Key = "asset-loans", Module = M.Assets, DeleteNeeds = Child, OrderBy = nameof(AssetLoan.FromDate), Related = s => x => x.BorrowerPersonId == s.PersonId },
        new ResourceDef<Maintenance> { Key = "maintenance", Module = M.Assets, DeleteNeeds = Child, OrderBy = nameof(Maintenance.Date) },
        new ResourceDef<Material> { Key = "materials", Module = M.Assets, SearchFields = ["NameTh", "NameEn", "Code"], OrderBy = nameof(Material.NameTh), OrderDescending = false },
        new ResourceDef<MaterialMovement> { Key = "material-movements", Module = M.Assets, DeleteNeeds = Child, OrderBy = nameof(MaterialMovement.Date), Related = s => x => x.PersonId == s.PersonId },

        // ── M7 booking resources (bookings themselves go through BookingsController)
        new ResourceDef<Resource> { Key = "resources", Module = M.Resources, OrderBy = nameof(Resource.NameTh), OrderDescending = false, Related = s => x => x.ManagerPersonId == s.PersonId },
        new ResourceDef<ResourceBlackout> { Key = "resource-blackouts", Module = M.Resources, DeleteNeeds = Child, OrderBy = nameof(ResourceBlackout.StartAt), Related = s => x => s.ManagedResourceIds.Contains(x.ResourceId) },

        // ── M8 tasks & meetings
        new ResourceDef<TaskItem>
        {
            Key = "tasks", Module = M.Tasks, SearchFields = ["Title", "Tags"], OrderBy = nameof(TaskItem.DueDate), OrderDescending = false,
            ServerFields = [nameof(TaskItem.CreatorPersonId)],
            Related = s => x => s.TaskIds.Contains(x.Id) || x.CreatorPersonId == s.PersonId,
            BeforeSave = (c, x, before) =>
            {
                if (before == null) x.CreatorPersonId = c.User.PersonId ?? "";
                if (x.Status == TaskState.Done && before?.Status != TaskState.Done) x.CompletedAt = DateTime.UtcNow;
                if (x.Status != TaskState.Done) x.CompletedAt = null;
                return Task.CompletedTask;
            },
        },
        new ResourceDef<TaskAssignee> { Key = "task-assignees", Module = M.Tasks, DeleteNeeds = Child, Related = s => x => s.TaskIds.Contains(x.TaskId) },
        new ResourceDef<TaskComment>
        {
            Key = "task-comments", Module = M.Tasks, DeleteNeeds = Child, OrderBy = nameof(TaskComment.CreatedAt), OrderDescending = false,
            ServerFields = [nameof(TaskComment.AuthorPersonId)], Related = s => x => s.TaskIds.Contains(x.TaskId),
            BeforeSave = (c, x, before) => { if (before == null) x.AuthorPersonId = c.User.PersonId ?? ""; return Task.CompletedTask; },
        },
        new ResourceDef<Meeting>
        {
            Key = "meetings", Module = M.Meetings, SearchFields = ["Title", "Location"], OrderBy = nameof(Meeting.StartAt),
            Related = s => x => s.MeetingIds.Contains(x.Id) || x.OrganizerPersonId == s.PersonId,
            BeforeSave = (c, x, before) =>
            {
                if (before == null && string.IsNullOrEmpty(x.OrganizerPersonId)) x.OrganizerPersonId = c.User.PersonId ?? "";
                if (x.EndAt < x.StartAt) throw new AppException("errors.endBeforeStart");
                return Task.CompletedTask;
            },
        },
        new ResourceDef<MeetingAttendee> { Key = "meeting-attendees", Module = M.Meetings, DeleteNeeds = Child, Related = s => x => s.MeetingIds.Contains(x.MeetingId) },
        new ResourceDef<MeetingMinutes>
        {
            Key = "meeting-minutes", Module = M.Meetings, DeleteNeeds = Child, ServerFields = [nameof(MeetingMinutes.RecordedByPersonId)],
            Related = s => x => s.MeetingIds.Contains(x.MeetingId),
            BeforeSave = (c, x, before) => { if (before == null) x.RecordedByPersonId = c.User.PersonId ?? ""; return Task.CompletedTask; },
        },

        // ── M9 documents
        new ResourceDef<Folder>
        {
            Key = "folders", Module = M.Documents, OrderBy = nameof(Folder.Name), OrderDescending = false, ServerFields = [nameof(Folder.OwnerPersonId)],
            Related = s => x => s.FolderIds.Contains(x.Id) || x.OwnerPersonId == s.PersonId,
            BeforeSave = (c, x, before) => { if (before == null) x.OwnerPersonId = c.User.PersonId ?? ""; return Task.CompletedTask; },
        },
        new ResourceDef<Document>
        {
            Key = "documents", Module = M.Documents, SearchFields = ["Title", "Tags"], OrderBy = nameof(Document.ModifiedAt), ServerFields = [nameof(Document.OwnerPersonId), nameof(Document.CurrentVersionId)],
            Related = s => x => s.DocumentIds.Contains(x.Id) || x.OwnerPersonId == s.PersonId,
            BeforeSave = (c, x, before) => { if (before == null) x.OwnerPersonId = c.User.PersonId ?? ""; return Task.CompletedTask; },
        },
        new ResourceDef<DocumentVersion> { Key = "document-versions", Module = M.Documents, ReadOnly = true, OrderBy = nameof(DocumentVersion.VersionNo), Related = s => x => s.DocumentIds.Contains(x.DocumentId) },
        new ResourceDef<Attachment> { Key = "attachments", Module = M.Documents, ReadOnly = true, Related = s => x => x.CreatedBy == s.UserId },

        // ── M10 site
        new ResourceDef<NewsPost>
        {
            Key = "news", Module = M.Cms, SearchFields = ["TitleTh", "TitleEn", "Slug"], OrderBy = nameof(NewsPost.PublishedAt),
            Related = s => x => x.AuthorPersonId == s.PersonId,
            BeforeSave = async (c, x, before) =>
            {
                if (before == null && x.AuthorPersonId == null) x.AuthorPersonId = c.User.PersonId;
                x.Slug = Slug.Make(string.IsNullOrWhiteSpace(x.Slug) ? (x.TitleEn ?? x.TitleTh) : x.Slug);
                if (await c.Db.Set<NewsPost>().AnyAsync(n => n.Slug == x.Slug && n.Id != x.Id)) x.Slug += "-" + Tokens.NewToken(3).ToLowerInvariant();
                // Publishing puts words on the group's public face: authors draft, editors publish.
                if (x.Status == PublishStatus.Published && before?.Status != PublishStatus.Published)
                {
                    if (!c.User.CanAll(M.Cms, PermAction.Edit)) throw new AppException("cms.publishNeedsEditor", 403);
                    x.PublishedAt ??= DateTime.UtcNow;
                }
            },
        },
        new ResourceDef<NewsLink> { Key = "news-links", Module = M.Cms, DeleteNeeds = Child },
        new ResourceDef<SitePage> { Key = "site-pages", Module = M.Cms, OrderBy = nameof(SitePage.Key), OrderDescending = false },
        new ResourceDef<SiteSetting> { Key = "site-settings", Module = M.Cms, OrderBy = nameof(SiteSetting.Key), OrderDescending = false },
        new ResourceDef<ContactMessage>
        {
            Key = "contact-messages", Module = M.Inbox, OrderBy = nameof(ContactMessage.SubmittedAt),
            ServerFields = [nameof(ContactMessage.Name), nameof(ContactMessage.Email), nameof(ContactMessage.Phone), nameof(ContactMessage.Subject),
                nameof(ContactMessage.Message), nameof(ContactMessage.ConsentRecordId), nameof(ContactMessage.SubmittedAt), nameof(ContactMessage.IpHash)],
            BeforeSave = (_, _, before) => before == null ? throw new AppException("contact.publicFormOnly") : Task.CompletedTask,
        },
    ];

    public static readonly Dictionary<string, ResourceDef> ByKey = All.ToDictionary(d => d.Key);
}
