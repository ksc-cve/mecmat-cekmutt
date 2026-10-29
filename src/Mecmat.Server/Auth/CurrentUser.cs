using Mecmat.Domain;

namespace Mecmat.Auth;

public readonly record struct Grant(PermAction Related, PermAction All)
{
    public static readonly Grant None = new(PermAction.None, PermAction.None);
    public static readonly Grant Full = new(PermAction.Manage, PermAction.Manage);
    public bool Allows(PermAction need, bool related) =>
        (related ? (PermAction)Math.Max((int)Related, (int)All) : All) >= need;
    public static Grant Max(Grant a, Grant b) =>
        new((PermAction)Math.Max((int)a.Related, (int)b.Related), (PermAction)Math.Max((int)a.All, (int)b.All));
}

/// <summary>The signed-in user for this request, with effective permissions already merged
/// (max over roles, then per-user overrides). Null fields mean an anonymous request.</summary>
public sealed class CurrentUser
{
    public string? UserId { get; set; }
    public string? PersonId { get; set; }
    public bool IsAdmin { get; set; }
    public string Language { get; set; } = "th";
    public Session? Session { get; set; }
    public UserAccount? Account { get; set; }
    public Dictionary<string, Grant> Grants { get; } = new();
    public bool IsAuthenticated => UserId != null;

    public Grant GrantFor(string module) =>
        IsAdmin ? Grant.Full
                : Grants.TryGetValue(module, out var g) ? g : Grant.None;

    public bool Can(string module, PermAction need, bool related = false) =>
        IsAuthenticated && GrantFor(module).Allows(need, related);

    public bool CanAll(string module, PermAction need) => Can(module, need, related: false);

    /// <summary>Whether the user can do anything at all in a module at this level, on some record.</summary>
    public bool CanAny(string module, PermAction need) => Can(module, need, related: true);
}
