namespace Mecmat.Auth;

/// <summary>Permission module keys (docs/authorization.md). A resource in the API maps to one
/// of these; the role matrix is expressed in these keys.</summary>
public static class Modules
{
    public const string People = "people";
    public const string Sensitive = "sensitive";
    public const string Organizations = "organizations";
    public const string Users = "users";
    public const string ApprovalsConfig = "approvals-config";
    public const string Publications = "publications";
    public const string Projects = "projects";
    public const string Finance = "finance";
    public const string Assets = "assets";
    public const string Resources = "resources";
    public const string Bookings = "bookings";
    public const string Tasks = "tasks";
    public const string Meetings = "meetings";
    public const string Documents = "documents";
    public const string Cms = "cms";
    public const string Inbox = "inbox";
    public const string Dashboard = "dashboard";
    public const string Audit = "audit";

    public static readonly string[] All =
    [
        People, Sensitive, Organizations, Users, ApprovalsConfig, Publications, Projects, Finance, Assets,
        Resources, Bookings, Tasks, Meetings, Documents, Cms, Inbox, Dashboard, Audit,
    ];
}
