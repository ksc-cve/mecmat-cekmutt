namespace Mecmat.Domain;

public enum TaskState { Todo, InProgress, Review, Done, Cancelled }
public enum Priority { Low, Normal, High, Urgent }
public enum MeetingKind { Meeting, Appointment, Seminar, ThesisDefense, SiteVisit, Other }
public enum MeetingStatus { Scheduled, Done, Cancelled }
public enum MeetingVisibility { Attendees, Group }
public enum AttendeeResponse { Pending, Accepted, Declined, Tentative }
public enum FolderVisibility { Private, Project, Group }
public enum DocumentKind { ManuscriptDraft, Report, Proposal, Image, Dataset, Presentation, Contract, Minutes, Other }

[IdPrefix("TSK")]
public class TaskItem : Entity
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ProjectId { get; set; }
    public string? ActivityId { get; set; }
    public string? ParentTaskId { get; set; }
    public string CreatorPersonId { get; set; } = "";
    public TaskState Status { get; set; } = TaskState.Todo;
    public Priority Priority { get; set; } = Priority.Normal;
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Tags { get; set; }
    public string? SourceMeetingId { get; set; }
}

[IdPrefix("TSA")]
public class TaskAssignee : Entity
{
    public string TaskId { get; set; } = "";
    public string PersonId { get; set; } = "";
}

[IdPrefix("TSC")]
public class TaskComment : Entity
{
    public string TaskId { get; set; } = "";
    public string AuthorPersonId { get; set; } = "";
    public string Body { get; set; } = "";
}

[IdPrefix("MTG")]
public class Meeting : Entity
{
    public string Title { get; set; } = "";
    public MeetingKind Kind { get; set; } = MeetingKind.Meeting;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Location { get; set; }
    public string? OnlineUrl { get; set; }
    public string? ProjectId { get; set; }
    public string OrganizerPersonId { get; set; } = "";
    public string? Agenda { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public MeetingVisibility Visibility { get; set; } = MeetingVisibility.Attendees;
}

[IdPrefix("MTA")]
public class MeetingAttendee : Entity
{
    public string MeetingId { get; set; } = "";
    public string? PersonId { get; set; }
    public string? ExternalName { get; set; }
    public AttendeeResponse Response { get; set; } = AttendeeResponse.Pending;
    public bool Attended { get; set; }
}

[IdPrefix("MTM")]
public class MeetingMinutes : Entity
{
    public string MeetingId { get; set; } = "";
    public string Body { get; set; } = "";
    public string RecordedByPersonId { get; set; } = "";
    public DateTime? ConfirmedAt { get; set; }
}

[IdPrefix("FLD")]
public class Folder : Entity
{
    public string Name { get; set; } = "";
    public string? ParentFolderId { get; set; }
    public string? ProjectId { get; set; }
    public string OwnerPersonId { get; set; } = "";
    public FolderVisibility Visibility { get; set; } = FolderVisibility.Private;
}

[IdPrefix("DOC")]
public class Document : Entity
{
    public string? FolderId { get; set; }
    public string Title { get; set; } = "";
    public DocumentKind Kind { get; set; } = DocumentKind.Other;
    public string? ProjectId { get; set; }
    public string? PublicationId { get; set; }
    public string? CurrentVersionId { get; set; }
    public string? Tags { get; set; }
    public FolderVisibility Visibility { get; set; } = FolderVisibility.Private;
    public string OwnerPersonId { get; set; } = "";
}

[IdPrefix("DCV")]
public class DocumentVersion : Entity
{
    public string DocumentId { get; set; } = "";
    public int VersionNo { get; set; }
    public string FileBlobId { get; set; } = "";
    public string? ChangeNote { get; set; }
}

[IdPrefix("FIL")]
public class FileBlob : Entity
{
    public string StoredName { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    /// <summary>Files attached to public records (photos, covers) may be served without login.</summary>
    public bool IsPublic { get; set; }
}
