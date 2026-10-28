namespace Mecmat.Domain;

/// <summary>Append-only audit row, written in the same SaveChanges as the change it records.
/// Not an <see cref="Entity"/>: audit rows are never edited or soft-deleted.</summary>
public class ChangeLog
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public string? UserId { get; set; }
    public string? PersonId { get; set; }
    public string EntityType { get; set; } = "";
    public string? EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpHash { get; set; }
}

public class SensitiveAccessLog
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public string ViewerUserId { get; set; } = "";
    public string SubjectPersonId { get; set; } = "";
    public string FieldKey { get; set; } = "";
}

[IdPrefix("NTF")]
public class Notification : Entity
{
    public string RecipientUserId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public string? Link { get; set; }
    public string? SubjectType { get; set; }
    public string? SubjectId { get; set; }
    /// <summary>Repeats of the same event about the same thing fold into one row ("X and N others").</summary>
    public string? GroupKey { get; set; }
    public int Count { get; set; } = 1;
    public DateTime? ReadAt { get; set; }
}

public class NotificationOutbox
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PayloadJson { get; set; } = "";
    public DateTime? ProcessedAt { get; set; }
}

[IdPrefix("CNS")]
public class ConsentRecord : Entity
{
    public string? SubjectPersonId { get; set; }
    public string? SubjectEmail { get; set; }
    public string Purpose { get; set; } = "";
    public string PolicyVersion { get; set; } = "";
    public DateTime GivenAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? IpHash { get; set; }
}

public class IdCounter
{
    public string Prefix { get; set; } = "";
    public string DateKey { get; set; } = "";
    public int LastValue { get; set; }
}

/// <summary>Remembers which time-based reminders already went out, so the 15-minute worker
/// never notifies the same due date twice.</summary>
public class ReminderSent
{
    public string Key { get; set; } = "";
    public DateTime SentAt { get; set; }
}
