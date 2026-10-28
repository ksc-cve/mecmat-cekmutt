namespace Mecmat.Domain;

public enum RecordStatus { Active, Archived, Deleted }

/// <summary>Prefix for server-generated ids (PFX-YYMMDD-####). Ids are never typed by hand,
/// so every entity that people refer to in conversation carries a readable prefix.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IdPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}

/// <summary>Marks a column whose plaintext must never leave the server unmasked
/// and never be written to ChangeLog (ADR 0007).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute;

public abstract class Entity
{
    public string Id { get; set; } = "";
    public RecordStatus RecordStatus { get; set; } = RecordStatus.Active;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    /// <summary>Optimistic concurrency token: a stale client gets 409 instead of silently
    /// overwriting someone else's edit.</summary>
    public long SyncStamp { get; set; }
}

/// <summary>Generic file attachment usable by any record (requests, assets, tasks …).</summary>
[IdPrefix("ATT")]
public class Attachment : Entity
{
    public string OwnerType { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string FileBlobId { get; set; } = "";
    public string? Label { get; set; }
}
