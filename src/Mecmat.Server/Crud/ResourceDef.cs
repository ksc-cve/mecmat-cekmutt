using System.Linq.Expressions;
using Mecmat.Auth;
using Mecmat.Data;
using Mecmat.Domain;

namespace Mecmat.Crud;

public sealed class CrudContext
{
    public required AppDbContext Db { get; init; }
    public required CurrentUser User { get; init; }
    public required Scope Scope { get; init; }
    public required IServiceProvider Services { get; init; }
}

/// <summary>Describes one API collection: which entity, which permission module, what "related"
/// means for it, and the hooks that keep its rules on the server (docs/authorization.md).</summary>
public abstract class ResourceDef
{
    public required string Key { get; init; }
    public required string Module { get; init; }
    public abstract Type EntityType { get; }
    /// <summary>Collections changed only through their own service (ledger, money requests …).</summary>
    public bool ReadOnly { get; init; }
    /// <summary>Child rows (education, authors, members …) are removed with Edit on the parent's
    /// module; top-level records need Manage.</summary>
    public PermAction DeleteNeeds { get; init; } = PermAction.Manage;
    /// <summary>Fields the client may never set; the server fills them (owner, creator …).</summary>
    public string[] ServerFields { get; init; } = [];
    public string[] SearchFields { get; init; } = [];
    public string OrderBy { get; init; } = nameof(Entity.CreatedAt);
    public bool OrderDescending { get; init; } = true;
}

public sealed class ResourceDef<T> : ResourceDef where T : Entity, new()
{
    public override Type EntityType => typeof(T);
    /// <summary>Null = nothing is ever "related"; the collection is governed by the All level only.</summary>
    public Func<Scope, Expression<Func<T, bool>>>? Related { get; init; }
    /// <summary>Runs before insert/update with the merged entity; throw AppException to refuse.</summary>
    public Func<CrudContext, T, T?, Task>? BeforeSave { get; init; }
    /// <summary>Runs after the row is saved, e.g. to add the creator as project PI.</summary>
    public Func<CrudContext, T, Task>? AfterCreate { get; init; }
}
