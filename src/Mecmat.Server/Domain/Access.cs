namespace Mecmat.Domain;

public enum AccountStatus { PendingApproval, Active, Suspended, Expired, Closed }
public enum PermAction { None = 0, View = 1, Create = 2, Edit = 3, Manage = 4 }
public enum ResetRequestStatus { Open, Done, Rejected }
public enum ApproverKind { Position, Person, ProjectRole, AccountCustodian, RequesterAdvisor, ResourceManager }
public enum StepRule { Any, All }
public enum ApprovalStatus { Pending, Approved, Rejected, Returned, Cancelled }
public enum ApprovalDecision { Approve, Reject, Return, Comment }

[IdPrefix("USR")]
public class UserAccount : Entity
{
    public string PersonId { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.PendingApproval;
    public DateOnly? ExpiresAt { get; set; }
    [Sensitive] public string? TotpSecret { get; set; }
    public bool TotpEnabled { get; set; }
    public string? RecoveryCodesHash { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string Language { get; set; } = "th";
    public string Theme { get; set; } = "auto";
    public bool IsAdmin { get; set; }
}

/// <summary>Server-side session. The cookie carries only a random token; the database holds
/// its SHA-256 so a leaked database cannot be replayed as live sessions (ADR 0003).</summary>
[IdPrefix("SES")]
public class Session : Entity
{
    public string TokenHash { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CsrfToken { get; set; } = "";
    public DateTime LastSeenAt { get; set; }
    public DateTime IdleExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public bool TotpPassed { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
}

[IdPrefix("PRR")]
public class PasswordResetRequest : Entity
{
    public string? UserId { get; set; }
    public string UsernameTyped { get; set; } = "";
    public string? Note { get; set; }
    public ResetRequestStatus Status { get; set; } = ResetRequestStatus.Open;
    public string? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
}

[IdPrefix("ROL")]
public class Role : Entity
{
    public string Key { get; set; } = "";
    public string NameTh { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

[IdPrefix("RPM")]
/// <summary>Two levels per module, because the matrix often says "edit what is yours, view
/// everything" (docs/authorization.md): RelatedAction applies to related records, AllAction to every record.</summary>
public class RolePermission : Entity
{
    public string RoleId { get; set; } = "";
    public string Module { get; set; } = "";
    public PermAction RelatedAction { get; set; }
    public PermAction AllAction { get; set; }
}

[IdPrefix("URL")]
public class UserRole : Entity
{
    public string UserId { get; set; } = "";
    public string RoleId { get; set; } = "";
}

[IdPrefix("UPO")]
public class UserPermissionOverride : Entity
{
    public string UserId { get; set; } = "";
    public string Module { get; set; } = "";
    public PermAction RelatedAction { get; set; }
    public PermAction AllAction { get; set; }
    public string? Reason { get; set; }
}

[IdPrefix("GPS")]
public class GroupPosition : Entity
{
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    /// <summary>Lower number = higher authority; used to order the public team page.</summary>
    public int Rank { get; set; } = 100;
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("PAS")]
public class PositionAssignment : Entity
{
    public string PositionId { get; set; } = "";
    public string PersonId { get; set; } = "";
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

[IdPrefix("APP")]
public class ApprovalPolicy : Entity
{
    public string Name { get; set; } = "";
    public string SubjectType { get; set; } = "";
    public string? RequestKind { get; set; }
    public long? MinAmount { get; set; }
    public long? MaxAmount { get; set; }
    public string? AccountScopeId { get; set; }
    public string? ProjectScopeId { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

[IdPrefix("APS")]
public class ApprovalPolicyStep : Entity
{
    public string PolicyId { get; set; } = "";
    public int StepOrder { get; set; }
    public ApproverKind ApproverKind { get; set; }
    public string? ApproverRef { get; set; }
    public StepRule Rule { get; set; } = StepRule.Any;
}

[IdPrefix("API")]
public class ApprovalInstance : Entity
{
    public string SubjectType { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string? PolicyId { get; set; }
    /// <summary>Steps resolved to people at submit time, so editing a policy later
    /// never reroutes a request already in flight (ADR 0005).</summary>
    public string PolicySnapshotJson { get; set; } = "[]";
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public int CurrentStep { get; set; } = 1;
    public string RequesterPersonId { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

[IdPrefix("APA")]
public class ApprovalAction : Entity
{
    public string InstanceId { get; set; } = "";
    public int StepOrder { get; set; }
    public string ActorPersonId { get; set; } = "";
    public string? OnBehalfOfPersonId { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime At { get; set; }
}

[IdPrefix("DLG")]
public class Delegation : Entity
{
    public string FromPersonId { get; set; } = "";
    public string ToPersonId { get; set; } = "";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? SubjectType { get; set; }
    public string? Reason { get; set; }
}
