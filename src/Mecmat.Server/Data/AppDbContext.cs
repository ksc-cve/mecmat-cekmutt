using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, AuditContext audit) : DbContext(options)
{
    /// <summary>One writer at a time inside this process. SQLite serialises writes anyway; the
    /// lock additionally keeps id counters and balance checks race-free without retries.</summary>
    public static readonly SemaphoreSlim WriteLock = new(1, 1);

    public static readonly Type[] EntityTypes = typeof(Entity).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Entity)))
        .OrderBy(t => t.Name).ToArray();

    public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
    public DbSet<SensitiveAccessLog> SensitiveAccessLogs => Set<SensitiveAccessLog>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<IdCounter> IdCounters => Set<IdCounter>();
    public DbSet<ReminderSent> RemindersSent => Set<ReminderSent>();

    /// <summary>Set by services that deliberately write without an audit row (e.g. session
    /// heartbeats), so routine bookkeeping does not drown the real history.</summary>
    public bool SkipAudit { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Enums as text: the database stays readable by hand and new values need no data migration.
        b.Properties<Enum>().HaveConversion<string>().HaveMaxLength(40);
        b.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        foreach (var t in EntityTypes)
        {
            var e = mb.Entity(t);
            e.HasKey(nameof(Entity.Id));
            e.Property(nameof(Entity.Id)).HasValueGenerator<EntityIdGenerator>().ValueGeneratedOnAdd();
            e.Property(nameof(Entity.SyncStamp)).IsConcurrencyToken();
            e.HasIndex(nameof(Entity.RecordStatus));
            foreach (var p in t.GetProperties().Where(p => p.GetCustomAttribute<SensitiveAttribute>() != null))
                e.Property(p.Name).HasConversion(new EncryptedStringConverter());
        }

        mb.Entity<ChangeLog>(e => { e.HasIndex(x => new { x.EntityType, x.EntityId }); e.HasIndex(x => x.At); });
        mb.Entity<SensitiveAccessLog>().HasIndex(x => x.SubjectPersonId);
        mb.Entity<IdCounter>().HasKey(x => new { x.Prefix, x.DateKey });
        mb.Entity<ReminderSent>().HasKey(x => x.Key);

        mb.Entity<UserAccount>().HasIndex(x => x.Username).IsUnique();
        mb.Entity<UserAccount>().HasIndex(x => x.PersonId).IsUnique();
        mb.Entity<Session>().HasIndex(x => x.TokenHash).IsUnique();
        mb.Entity<Role>().HasIndex(x => x.Key).IsUnique();
        mb.Entity<Person>().HasIndex(x => x.PublicSlug).IsUnique().HasFilter("PublicSlug IS NOT NULL");
        mb.Entity<NewsPost>().HasIndex(x => x.Slug).IsUnique();
        mb.Entity<SitePage>().HasIndex(x => x.Key).IsUnique();
        mb.Entity<SiteSetting>().HasIndex(x => x.Key).IsUnique();
        mb.Entity<Account>().HasIndex(x => x.Code).IsUnique();
        mb.Entity<PublicFieldVisibility>().HasIndex(x => new { x.PersonId, x.FieldKey }).IsUnique();
        mb.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.Module }).IsUnique();
        mb.Entity<Booking>().HasIndex(x => new { x.ResourceId, x.StartAt });
        mb.Entity<Attachment>().HasIndex(x => new { x.OwnerType, x.OwnerId });
        mb.Entity<ResearchAreaLink>().HasIndex(x => new { x.TargetType, x.TargetId });
        mb.Entity<NewsLink>().HasIndex(x => new { x.TargetType, x.TargetId });
        mb.Entity<Notification>().HasIndex(x => new { x.RecipientUserId, x.ReadAt });
        mb.Entity<LedgerEntry>().HasIndex(x => x.EntryDate);

        // Foreign keys. Restrict everywhere: rows are soft-deleted, so a hard delete that
        // would orphan children is always a bug and should fail loudly.
        Fk<Person, Organization>(mb, x => x.OrganizationId);
        Fk<Person, Person>(mb, x => x.AdvisorPersonId);
        Fk<Person, FileBlob>(mb, x => x.PhotoFileId);
        Fk<PersonEducation, Person>(mb, x => x.PersonId);
        Fk<PersonEmployment, Person>(mb, x => x.PersonId);
        Fk<PersonEmployment, Organization>(mb, x => x.OrganizationId);
        Fk<AcademicAppointment, Person>(mb, x => x.PersonId);
        Fk<TeachingRecord, Person>(mb, x => x.PersonId);
        Fk<ProfessionalLicense, Person>(mb, x => x.PersonId);
        Fk<PayoutChannel, Person>(mb, x => x.PersonId);
        Fk<PublicFieldVisibility, Person>(mb, x => x.PersonId);
        Fk<Organization, Organization>(mb, x => x.ParentOrganizationId);
        Fk<Award, Project>(mb, x => x.ProjectId);
        Fk<Award, Publication>(mb, x => x.PublicationId);
        Fk<AwardRecipient, Award>(mb, x => x.AwardId);
        Fk<AwardRecipient, Person>(mb, x => x.PersonId);
        Fk<ResearchAreaLink, ResearchArea>(mb, x => x.ResearchAreaId);

        Fk<UserAccount, Person>(mb, x => x.PersonId);
        Fk<Session, UserAccount>(mb, x => x.UserId);
        Fk<PasswordResetRequest, UserAccount>(mb, x => x.UserId);
        Fk<RolePermission, Role>(mb, x => x.RoleId);
        Fk<UserRole, UserAccount>(mb, x => x.UserId);
        Fk<UserRole, Role>(mb, x => x.RoleId);
        Fk<UserPermissionOverride, UserAccount>(mb, x => x.UserId);
        Fk<PositionAssignment, GroupPosition>(mb, x => x.PositionId);
        Fk<PositionAssignment, Person>(mb, x => x.PersonId);
        Fk<ApprovalPolicy, Account>(mb, x => x.AccountScopeId);
        Fk<ApprovalPolicy, Project>(mb, x => x.ProjectScopeId);
        Fk<ApprovalPolicyStep, ApprovalPolicy>(mb, x => x.PolicyId);
        Fk<ApprovalAction, ApprovalInstance>(mb, x => x.InstanceId);
        Fk<ApprovalAction, Person>(mb, x => x.ActorPersonId);
        Fk<Delegation, Person>(mb, x => x.FromPersonId);
        Fk<Delegation, Person>(mb, x => x.ToPersonId);

        Fk<PublicationAuthor, Publication>(mb, x => x.PublicationId);
        Fk<PublicationAuthor, Person>(mb, x => x.PersonId);
        Fk<PublicationIndexing, Publication>(mb, x => x.PublicationId);
        Fk<PublicationUsage, Publication>(mb, x => x.PublicationId);
        Fk<PublicationUsage, Person>(mb, x => x.PersonId);
        Fk<PublicationProject, Publication>(mb, x => x.PublicationId);
        Fk<PublicationProject, Project>(mb, x => x.ProjectId);

        Fk<Project, Project>(mb, x => x.ParentProjectId);
        Fk<Project, Organization>(mb, x => x.FunderOrganizationId);
        Fk<ProjectMember, Project>(mb, x => x.ProjectId);
        Fk<ProjectMember, Person>(mb, x => x.PersonId);
        Fk<ProjectPartner, Project>(mb, x => x.ProjectId);
        Fk<ProjectPartner, Organization>(mb, x => x.OrganizationId);
        Fk<Activity, Project>(mb, x => x.ProjectId);
        Fk<Activity, Activity>(mb, x => x.ParentActivityId);
        Fk<Milestone, Project>(mb, x => x.ProjectId);
        Fk<ProjectOutput, Project>(mb, x => x.ProjectId);
        Fk<ProjectOutcome, Project>(mb, x => x.ProjectId);
        Fk<ProjectImpact, Project>(mb, x => x.ProjectId);
        Fk<KeyResult, Project>(mb, x => x.ProjectId);
        Fk<ProgressReport, Project>(mb, x => x.ProjectId);
        Fk<Recruitment, Project>(mb, x => x.ProjectId);
        Fk<Application, Recruitment>(mb, x => x.RecruitmentId);

        Fk<Account, Account>(mb, x => x.ParentAccountId);
        Fk<Account, Project>(mb, x => x.ProjectId);
        Fk<Account, Person>(mb, x => x.CustodianPersonId);
        Fk<AccountBudget, Account>(mb, x => x.AccountId);
        Fk<LedgerEntry, Account>(mb, x => x.FromAccountId);
        Fk<LedgerEntry, Account>(mb, x => x.ToAccountId);
        Fk<LedgerEntry, MoneyRequest>(mb, x => x.MoneyRequestId);
        Fk<LedgerEntry, LedgerEntry>(mb, x => x.ReversesEntryId);
        Fk<MoneyRequest, Person>(mb, x => x.RequesterPersonId);
        Fk<MoneyRequest, Account>(mb, x => x.AccountId);
        Fk<MoneyRequest, Account>(mb, x => x.ToAccountId);
        Fk<MoneyRequest, Project>(mb, x => x.ProjectId);
        Fk<MoneyRequest, MoneyRequest>(mb, x => x.RelatedAdvanceId);
        Fk<MoneyRequestLine, MoneyRequest>(mb, x => x.RequestId);

        Fk<Asset, Project>(mb, x => x.FundingProjectId);
        Fk<Asset, Account>(mb, x => x.FundingAccountId);
        Fk<Asset, Person>(mb, x => x.CustodianPersonId);
        Fk<Asset, Organization>(mb, x => x.VendorOrganizationId);
        Fk<AssetLoan, Asset>(mb, x => x.AssetId);
        Fk<AssetLoan, Person>(mb, x => x.BorrowerPersonId);
        Fk<Maintenance, Asset>(mb, x => x.AssetId);
        Fk<MaterialMovement, Material>(mb, x => x.MaterialId);
        Fk<MaterialMovement, Project>(mb, x => x.ProjectId);

        Fk<Resource, Asset>(mb, x => x.AssetId);
        Fk<Resource, Person>(mb, x => x.ManagerPersonId);
        Fk<Booking, Resource>(mb, x => x.ResourceId);
        Fk<Booking, Person>(mb, x => x.BookerPersonId);
        Fk<Booking, Project>(mb, x => x.ProjectId);
        Fk<ResourceBlackout, Resource>(mb, x => x.ResourceId);

        Fk<TaskItem, Project>(mb, x => x.ProjectId);
        Fk<TaskItem, Activity>(mb, x => x.ActivityId);
        Fk<TaskItem, TaskItem>(mb, x => x.ParentTaskId);
        Fk<TaskItem, Meeting>(mb, x => x.SourceMeetingId);
        Fk<TaskAssignee, TaskItem>(mb, x => x.TaskId);
        Fk<TaskAssignee, Person>(mb, x => x.PersonId);
        Fk<TaskComment, TaskItem>(mb, x => x.TaskId);
        Fk<Meeting, Project>(mb, x => x.ProjectId);
        Fk<Meeting, Person>(mb, x => x.OrganizerPersonId);
        Fk<MeetingAttendee, Meeting>(mb, x => x.MeetingId);
        Fk<MeetingAttendee, Person>(mb, x => x.PersonId);
        Fk<MeetingMinutes, Meeting>(mb, x => x.MeetingId);
        Fk<Folder, Folder>(mb, x => x.ParentFolderId);
        Fk<Folder, Project>(mb, x => x.ProjectId);
        Fk<Document, Folder>(mb, x => x.FolderId);
        Fk<Document, Project>(mb, x => x.ProjectId);
        Fk<Document, Publication>(mb, x => x.PublicationId);
        Fk<DocumentVersion, Document>(mb, x => x.DocumentId);
        Fk<DocumentVersion, FileBlob>(mb, x => x.FileBlobId);
        Fk<Attachment, FileBlob>(mb, x => x.FileBlobId);

        Fk<NewsLink, NewsPost>(mb, x => x.NewsPostId);
        Fk<Application, ConsentRecord>(mb, x => x.ConsentRecordId);
        Fk<ContactMessage, ConsentRecord>(mb, x => x.ConsentRecordId);
        Fk<Notification, UserAccount>(mb, x => x.RecipientUserId);
    }

    private static void Fk<TDep, TPrin>(ModelBuilder mb, Expression<Func<TDep, string?>> fk)
        where TDep : class where TPrin : class =>
        mb.Entity<TDep>().HasOne<TPrin>().WithMany()
            .HasForeignKey(((MemberExpression)fk.Body).Member.Name).OnDelete(DeleteBehavior.Restrict);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("Use SaveChangesAsync so ids, stamps and audit rows are applied.");

    private bool _holdsLock;

    /// <summary>Runs a read-check-write sequence (balance check, overlap check) as one unit under
    /// the process write lock. SaveChangesAsync inside it does not wait for the lock again.</summary>
    public async Task ExclusiveAsync(Func<Task> work)
    {
        if (_holdsLock) { await work(); return; }
        await WriteLock.WaitAsync();
        _holdsLock = true;
        try { await work(); }
        finally { _holdsLock = false; WriteLock.Release(); }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        var locked = !_holdsLock;
        if (locked) await WriteLock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var logs = new List<(EntityEntry Entry, ChangeLog Log)>();
            foreach (var entry in ChangeTracker.Entries<Entity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        await PersistCounterAsync(entry.Entity.Id, ct);
                        entry.Entity.CreatedAt = entry.Entity.ModifiedAt = now;
                        entry.Entity.CreatedBy = entry.Entity.ModifiedBy = audit.UserId;
                        entry.Entity.SyncStamp = 1;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedAt = now;
                        entry.Entity.ModifiedBy = audit.UserId;
                        entry.Entity.SyncStamp++;
                        break;
                    case EntityState.Deleted:
                        throw new InvalidOperationException(
                            $"{entry.Entity.GetType().Name} rows are soft-deleted (RecordStatus=Deleted), never removed.");
                }
                if (!SkipAudit && entry.State is EntityState.Added or EntityState.Modified)
                    logs.Add((entry, BuildLog(entry, now)));
            }
            foreach (var (entry, log) in logs)
            {
                log.EntityId = entry.Entity is Entity en ? en.Id : null;
                ChangeLogs.Add(log);
            }
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
        }
        finally { if (locked) WriteLock.Release(); }
    }

    /// <summary>Records an event that is not a row change (sign-in, failed sign-in, approval
    /// decision …). It is saved with the next SaveChangesAsync, in the same commit.</summary>
    public void LogEvent(string entityType, string? entityId, string action, object? detail = null) =>
        ChangeLogs.Add(new ChangeLog
        {
            At = DateTime.UtcNow, UserId = audit.UserId, PersonId = audit.PersonId, IpHash = audit.IpHash,
            EntityType = entityType, EntityId = entityId, Action = action,
            AfterJson = detail == null ? null : JsonSerializer.Serialize(detail, Json.Options),
        });

    private ChangeLog BuildLog(EntityEntry entry, DateTime now)
    {
        var type = entry.Entity.GetType();
        var before = new Dictionary<string, object?>();
        var after = new Dictionary<string, object?>();
        foreach (var p in entry.Properties)
        {
            var name = p.Metadata.Name;
            if (name is nameof(Entity.ModifiedAt) or nameof(Entity.ModifiedBy) or nameof(Entity.SyncStamp)) continue;
            if (entry.State == EntityState.Modified && !p.IsModified) continue;
            var sensitive = type.GetProperty(name)?.GetCustomAttribute<SensitiveAttribute>() != null
                            || name == nameof(UserAccount.PasswordHash) || name == nameof(UserAccount.RecoveryCodesHash)
                            || name == nameof(Session.TokenHash) || name == nameof(Session.CsrfToken);
            // Secrets and encrypted columns are recorded as "changed", never by value (ADR 0007).
            object? Mask(object? v) => sensitive ? (v == null ? null : "•changed•") : v;
            if (entry.State == EntityState.Modified)
            {
                if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                before[name] = Mask(p.OriginalValue);
            }
            after[name] = Mask(p.CurrentValue);
        }
        var action = entry.State == EntityState.Added ? "Create"
            : after.TryGetValue(nameof(Entity.RecordStatus), out var rs) && rs is RecordStatus.Deleted ? "Delete"
            : before.TryGetValue(nameof(Entity.RecordStatus), out var ob) && ob is RecordStatus.Deleted ? "Restore"
            : "Update";
        return new ChangeLog
        {
            At = now, UserId = audit.UserId, PersonId = audit.PersonId, IpHash = audit.IpHash,
            EntityType = type.Name, Action = action,
            BeforeJson = before.Count == 0 ? null : JsonSerializer.Serialize(before, Json.Options),
            AfterJson = JsonSerializer.Serialize(after, Json.Options),
        };
    }

    /// <summary>Keeps IdCounter in step with ids handed out in memory, so numbering continues
    /// after a restart instead of starting again at 0001.</summary>
    private async Task PersistCounterAsync(string id, CancellationToken ct)
    {
        var parts = id.Split('-');
        if (parts.Length != 3 || !int.TryParse(parts[2], out var n)) return;
        var counter = IdCounters.Local.FirstOrDefault(c => c.Prefix == parts[0] && c.DateKey == parts[1])
                      ?? await IdCounters.FirstOrDefaultAsync(c => c.Prefix == parts[0] && c.DateKey == parts[1], ct);
        if (counter == null) IdCounters.Add(counter = new IdCounter { Prefix = parts[0], DateKey = parts[1] });
        if (n > counter.LastValue) counter.LastValue = n;
    }
}
