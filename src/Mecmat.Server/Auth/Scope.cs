using Microsoft.EntityFrameworkCore;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Auth;

/// <summary>Everything "Related" depends on for one user, loaded once per request. The group is
/// small (tens of people, tens of projects), so plain id sets are cheaper than joins.</summary>
public sealed class Scope
{
    public string PersonId { get; init; } = "";
    public string UserId { get; init; } = "";
    public HashSet<string> ProjectIds { get; init; } = [];
    public HashSet<string> LeadProjectIds { get; init; } = [];
    public HashSet<string> FinanceProjectIds { get; init; } = [];
    public HashSet<string> AdviseeIds { get; init; } = [];
    /// <summary>People this user may treat as "related": self, advisees, members of projects they lead.</summary>
    public HashSet<string> RelatedPersonIds { get; init; } = [];
    public HashSet<string> PublicationIds { get; init; } = [];
    /// <summary>Accounts the user looks after or that belong to projects they lead, with all descendants.</summary>
    public HashSet<string> AccountIds { get; init; } = [];
    public HashSet<string> MeetingIds { get; init; } = [];
    public HashSet<string> TaskIds { get; init; } = [];
    public HashSet<string> FolderIds { get; init; } = [];
    public HashSet<string> DocumentIds { get; init; } = [];
    public HashSet<string> ManagedResourceIds { get; init; } = [];
    public HashSet<string> ApprovalSubjectIds { get; init; } = [];

    public static readonly Scope Empty = new();

    public static async Task<Scope> LoadAsync(AppDbContext db, string userId, string personId, CancellationToken ct = default)
    {
        var members = await db.Set<ProjectMember>().AsNoTracking()
            .Where(m => m.PersonId == personId && m.RecordStatus == RecordStatus.Active)
            .Select(m => new { m.ProjectId, m.Role }).ToListAsync(ct);
        var projectIds = members.Select(m => m.ProjectId).ToHashSet();
        var lead = members.Where(m => m.Role is ProjectRole.PI or ProjectRole.CoPI).Select(m => m.ProjectId).ToHashSet();
        var finance = members.Where(m => m.Role is ProjectRole.PI or ProjectRole.CoPI or ProjectRole.FinanceContact)
            .Select(m => m.ProjectId).ToHashSet();
        // Sub-projects inherit membership of the programme above them for reading.
        var children = await db.Set<Project>().AsNoTracking()
            .Where(p => p.ParentProjectId != null && projectIds.Contains(p.ParentProjectId))
            .Select(p => p.Id).ToListAsync(ct);
        projectIds.UnionWith(children);

        var advisees = await db.Set<Person>().AsNoTracking()
            .Where(p => p.AdvisorPersonId == personId).Select(p => p.Id).ToListAsync(ct);
        var leadMembers = await db.Set<ProjectMember>().AsNoTracking()
            .Where(m => lead.Contains(m.ProjectId)).Select(m => m.PersonId).ToListAsync(ct);
        var related = new HashSet<string> { personId };
        related.UnionWith(advisees);
        related.UnionWith(leadMembers);

        var pubs = await db.Set<PublicationAuthor>().AsNoTracking()
            .Where(a => a.PersonId == personId).Select(a => a.PublicationId).ToListAsync(ct);
        var projPubs = await db.Set<PublicationProject>().AsNoTracking()
            .Where(pp => projectIds.Contains(pp.ProjectId)).Select(pp => pp.PublicationId).ToListAsync(ct);

        var accounts = await db.Set<Account>().AsNoTracking()
            .Select(a => new { a.Id, a.ParentAccountId, a.CustodianPersonId, a.ProjectId }).ToListAsync(ct);
        var roots = accounts.Where(a => a.CustodianPersonId == personId || (a.ProjectId != null && finance.Contains(a.ProjectId)))
            .Select(a => a.Id).ToList();
        var accountIds = new HashSet<string>();
        var stack = new Stack<string>(roots);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!accountIds.Add(id)) continue;
            foreach (var c in accounts.Where(a => a.ParentAccountId == id)) stack.Push(c.Id);
        }

        var meetings = await db.Set<MeetingAttendee>().AsNoTracking()
            .Where(a => a.PersonId == personId).Select(a => a.MeetingId).ToListAsync(ct);
        meetings.AddRange(await db.Set<Meeting>().AsNoTracking()
            .Where(m => m.OrganizerPersonId == personId || m.Visibility == MeetingVisibility.Group
                        || (m.ProjectId != null && projectIds.Contains(m.ProjectId)))
            .Select(m => m.Id).ToListAsync(ct));
        var tasks = await db.Set<TaskAssignee>().AsNoTracking()
            .Where(a => a.PersonId == personId).Select(a => a.TaskId).ToListAsync(ct);
        tasks.AddRange(await db.Set<TaskItem>().AsNoTracking()
            .Where(t => t.CreatorPersonId == personId || (t.ProjectId != null && projectIds.Contains(t.ProjectId)))
            .Select(t => t.Id).ToListAsync(ct));
        var folders = await db.Set<Folder>().AsNoTracking()
            .Where(f => f.OwnerPersonId == personId || f.Visibility == FolderVisibility.Group
                        || (f.Visibility == FolderVisibility.Project && f.ProjectId != null && projectIds.Contains(f.ProjectId)))
            .Select(f => f.Id).ToListAsync(ct);

        var docs = await db.Set<Document>().AsNoTracking()
            .Where(d => d.OwnerPersonId == personId || d.Visibility == FolderVisibility.Group
                        || (d.Visibility == FolderVisibility.Project && d.ProjectId != null && projectIds.Contains(d.ProjectId))
                        || (d.FolderId != null && folders.Contains(d.FolderId)))
            .Select(d => d.Id).ToListAsync(ct);
        var managed = await db.Set<Resource>().AsNoTracking()
            .Where(r => r.ManagerPersonId == personId).Select(r => r.Id).ToListAsync(ct);

        var pending = await db.Set<ApprovalInstance>().AsNoTracking()
            .Where(i => i.Status == ApprovalStatus.Pending).Select(i => new { i.SubjectId, i.PolicySnapshotJson }).ToListAsync(ct);
        var approvalSubjects = pending.Where(p => p.PolicySnapshotJson.Contains(personId)).Select(p => p.SubjectId).ToHashSet();

        return new Scope
        {
            PersonId = personId, UserId = userId, ProjectIds = projectIds, LeadProjectIds = lead, FinanceProjectIds = finance,
            AdviseeIds = advisees.ToHashSet(), RelatedPersonIds = related,
            PublicationIds = pubs.Concat(projPubs).ToHashSet(), AccountIds = accountIds,
            MeetingIds = meetings.ToHashSet(), TaskIds = tasks.ToHashSet(), FolderIds = folders.ToHashSet(),
            DocumentIds = docs.ToHashSet(), ManagedResourceIds = managed.ToHashSet(),
            ApprovalSubjectIds = approvalSubjects,
        };
    }
}
