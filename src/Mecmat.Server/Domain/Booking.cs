namespace Mecmat.Domain;

public enum ResourceKind { Equipment, Room, Vehicle, Other }
public enum BookingStatus { Pending, Confirmed, Rejected, Cancelled, Completed, NoShow }

[IdPrefix("RES")]
public class Resource : Entity
{
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public ResourceKind Kind { get; set; } = ResourceKind.Equipment;
    public string? AssetId { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ManagerPersonId { get; set; }
    public bool RequiresApproval { get; set; }
    public int SlotMinutes { get; set; } = 30;
    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }
    public int? MaxAdvanceDays { get; set; }
    /// <summary>JSON array of 7 entries (Mon..Sun), each null (closed) or ["08:00","17:00"].</summary>
    public string? OpenHoursJson { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}

[IdPrefix("BKG")]
public class Booking : Entity
{
    public string ResourceId { get; set; } = "";
    public string BookerPersonId { get; set; } = "";
    public string? ProjectId { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Note { get; set; }
    public string? RecurrenceGroupId { get; set; }
    public string? ApprovalInstanceId { get; set; }
    public string? CancelledReason { get; set; }
}

[IdPrefix("RBO")]
public class ResourceBlackout : Entity
{
    public string ResourceId { get; set; } = "";
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Reason { get; set; }
}
