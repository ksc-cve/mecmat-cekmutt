namespace Mecmat.Data;

/// <summary>Who is acting in this request. Filled by the session middleware; background
/// workers leave it empty, which the audit trail records as "system".</summary>
public sealed class AuditContext
{
    public string? UserId { get; set; }
    public string? PersonId { get; set; }
    public string? IpHash { get; set; }
}
