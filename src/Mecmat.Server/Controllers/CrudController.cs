using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Crud;
using Mecmat.Data;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Controllers;

/// <summary>One controller for every collection in <see cref="Registry"/>. Permission, scope and the
/// per-resource hooks are applied here so no module can forget them.</summary>
[ApiController, Route("api/r/{key}")]
public sealed class CrudController(AppDbContext db, CurrentUser cu, IServiceProvider sp) : ControllerBase
{
    private static readonly HashSet<string> BaseFields =
    [
        nameof(Entity.Id), nameof(Entity.RecordStatus), nameof(Entity.CreatedAt), nameof(Entity.CreatedBy),
        nameof(Entity.ModifiedAt), nameof(Entity.ModifiedBy), nameof(Entity.SyncStamp),
    ];

    private static readonly MethodInfo ListM = typeof(CrudController).GetMethod(nameof(ListCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo GetM = typeof(CrudController).GetMethod(nameof(GetCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo SaveM = typeof(CrudController).GetMethod(nameof(SaveCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo DeleteM = typeof(CrudController).GetMethod(nameof(DeleteCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private ResourceDef Def(string key)
    {
        if (!cu.IsAuthenticated) throw new AppException("auth.required", 401);
        return Registry.ByKey.TryGetValue(key, out var d) ? d : throw new AppException("errors.notFound", 404);
    }

    private Task<IActionResult> Call(MethodInfo m, ResourceDef d, params object?[] args) =>
        (Task<IActionResult>)m.MakeGenericMethod(d.EntityType).Invoke(this, [d, .. args])!;

    [HttpGet]
    public Task<IActionResult> List(string key) => Call(ListM, Def(key));

    [HttpGet("{id}")]
    public Task<IActionResult> Get(string key, string id) => Call(GetM, Def(key), id);

    [HttpPost]
    public Task<IActionResult> Create(string key, [FromBody] JsonObject body) => Call(SaveM, Def(key), null, body);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(string key, string id, [FromBody] JsonObject body) => Call(SaveM, Def(key), id, body);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(string key, string id) => Call(DeleteM, Def(key), id, RecordStatus.Deleted);

    [HttpPost("{id}/restore")]
    public Task<IActionResult> Restore(string key, string id) => Call(DeleteM, Def(key), id, RecordStatus.Active);

    private async Task<Scope> ScopeAsync() =>
        HttpContext.Items["scope"] as Scope ?? (Scope)(HttpContext.Items["scope"] = await Scope.LoadAsync(db, cu.UserId!, cu.PersonId!));

    private CrudContext Ctx(Scope s) => new() { Db = db, User = cu, Scope = s, Services = sp };

    /// <summary>Applies the read rule: everything with View on All, related rows only with View on
    /// Related, otherwise refuse. The filter is in the query, never applied after loading.</summary>
    private async Task<IQueryable<T>> VisibleAsync<T>(ResourceDef<T> d, bool includeDeleted) where T : Entity, new()
    {
        var g = cu.GrantFor(d.Module);
        IQueryable<T> q = db.Set<T>().AsNoTracking();
        if (!includeDeleted) q = q.Where(x => x.RecordStatus != RecordStatus.Deleted);
        if (g.All >= PermAction.View) return q;
        if (g.Related >= PermAction.View && d.Related != null) return q.Where(d.Related(await ScopeAsync()));
        throw new AppException("errors.forbidden", 403);
    }

    private async Task<IActionResult> ListCore<T>(ResourceDef d0) where T : Entity, new()
    {
        var d = (ResourceDef<T>)d0;
        var query = Request.Query;
        var showDeleted = query["deleted"] == "1" && cu.Can(d.Module, PermAction.Manage);
        var q = await VisibleAsync(d, showDeleted);
        if (showDeleted) q = q.Where(x => x.RecordStatus == RecordStatus.Deleted);

        // Equality filters: ?ProjectId=PRJ-…&Status=Active (any public property).
        foreach (var (k, v) in query)
        {
            if (k is "q" or "take" or "skip" or "deleted" or "order") continue;
            var prop = typeof(T).GetProperty(k, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null || prop.GetCustomAttribute<SensitiveAttribute>() != null) continue;
            q = q.Where(EqualsExpr<T>(prop, v.ToString()));
        }
        var text = query["q"].ToString().Trim();
        if (text.Length > 0 && d.SearchFields.Length > 0) q = q.Where(SearchExpr<T>(d.SearchFields, text));

        var total = await q.CountAsync();
        var orderProp = query["order"].ToString() is { Length: > 0 } o && typeof(T).GetProperty(o.TrimStart('-')) != null ? o : null;
        var desc = orderProp?.StartsWith('-') ?? d.OrderDescending;
        q = OrderExpr(q, orderProp?.TrimStart('-') ?? d.OrderBy, desc);
        var take = Math.Clamp(int.TryParse(query["take"], out var t) ? t : 200, 1, 1000);
        var skip = Math.Max(0, int.TryParse(query["skip"], out var sk) ? sk : 0);
        var rows = await q.Skip(skip).Take(take).ToListAsync();
        var scope = cu.GrantFor(d.Module).All >= PermAction.Edit || d.Related == null ? null : await ScopeAsync();
        return Ok(new { total, rows = rows.Select(r => Shape(d, r, scope)) });
    }

    private async Task<IActionResult> GetCore<T>(ResourceDef d0, string id) where T : Entity, new()
    {
        var d = (ResourceDef<T>)d0;
        var q = await VisibleAsync(d, includeDeleted: cu.Can(d.Module, PermAction.Manage));
        var row = await q.FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        var scope = d.Related == null ? null : await ScopeAsync();
        return Ok(Shape(d, row, scope));
    }

    /// <summary>Row as JSON plus "_can" flags, so the UI shows only the buttons the server will accept.
    /// Sensitive columns are always masked here; the full value needs the reveal endpoint.</summary>
    private JsonObject Shape<T>(ResourceDef<T> d, T row, Scope? scope) where T : Entity, new()
    {
        var node = JsonSerializer.SerializeToNode(row, Json.Options)!.AsObject();
        foreach (var p in typeof(T).GetProperties().Where(p => p.GetCustomAttribute<SensitiveAttribute>() != null))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(p.Name);
            node[name] = FieldCipher.Mask(p.GetValue(row) as string);
        }
        var related = scope != null && d.Related != null && d.Related(scope).Compile()(row);
        var g = cu.GrantFor(d.Module);
        node["_can"] = new JsonObject
        {
            ["edit"] = !d.ReadOnly && g.Allows(PermAction.Edit, related),
            ["delete"] = !d.ReadOnly && g.Allows(d.DeleteNeeds, related),
        };
        return node;
    }

    private async Task<IActionResult> SaveCore<T>(ResourceDef d0, string? id, JsonObject body) where T : Entity, new()
    {
        var d = (ResourceDef<T>)d0;
        if (d.ReadOnly) throw new AppException("errors.readOnly", 405);
        var scope = await ScopeAsync();
        var ctx = Ctx(scope);
        var isRelated = d.Related == null ? null : d.Related(scope).Compile();
        var g = cu.GrantFor(d.Module);

        T entity;
        T? before = null;
        if (id == null)
        {
            entity = new T();
            Apply(d, entity, body);
            if (!g.Allows(PermAction.Create, isRelated?.Invoke(entity) ?? false)) throw new AppException("errors.forbidden", 403);
            db.Add(entity);
        }
        else
        {
            entity = await db.Set<T>().FirstOrDefaultAsync(x => x.Id == id && x.RecordStatus != RecordStatus.Deleted)
                     ?? throw new AppException("errors.notFound", 404);
            if (!g.Allows(PermAction.Edit, isRelated?.Invoke(entity) ?? false)) throw new AppException("errors.forbidden", 403);
            before = (T)db.Entry(entity).OriginalValues.ToObject();
            if (body.TryGetPropertyValue("syncStamp", out var st) && st != null)
                db.Entry(entity).Property(nameof(Entity.SyncStamp)).OriginalValue = st.GetValue<long>();
            Apply(d, entity, body);
            // The edited row must still be one the user may hold — e.g. a researcher cannot move an
            // education entry onto someone else's profile.
            if (!g.Allows(PermAction.Edit, isRelated?.Invoke(entity) ?? false)) throw new AppException("errors.forbidden", 403);
        }
        await RequireSensitiveRightsAsync(entity, body, scope);
        if (d.BeforeSave != null) await d.BeforeSave(ctx, entity, before);
        await db.SaveChangesAsync();
        if (id == null && d.AfterCreate != null) await d.AfterCreate(ctx, entity);
        HttpContext.Items.Remove("scope");
        return Ok(Shape(d, entity, await ScopeAsync()));
    }

    /// <summary>Writing an encrypted column (national ID, account number) needs the Sensitive module,
    /// on top of the record's own module.</summary>
    private Task RequireSensitiveRightsAsync<T>(T entity, JsonObject body, Scope scope)
    {
        var touched = typeof(T).GetProperties().Any(p => p.GetCustomAttribute<SensitiveAttribute>() != null
            && body.ContainsKey(JsonNamingPolicy.CamelCase.ConvertName(p.Name)));
        if (!touched) return Task.CompletedTask;
        var personId = entity switch { Person p => p.Id, PayoutChannel pc => pc.PersonId, _ => null };
        var own = personId != null && (personId == scope.PersonId || (entity is Person && string.IsNullOrEmpty(personId)));
        if (!cu.Can(Modules.Sensitive, PermAction.Edit, own)) throw new AppException("errors.forbidden", 403);
        return Task.CompletedTask;
    }

    private void Apply<T>(ResourceDef<T> d, T entity, JsonObject body) where T : Entity, new()
    {
        foreach (var (name, value) in body)
        {
            var prop = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null || !prop.CanWrite || BaseFields.Contains(prop.Name) || d.ServerFields.Contains(prop.Name)) continue;
            // A masked value coming back from the form means "unchanged", not "store these dots".
            if (prop.GetCustomAttribute<SensitiveAttribute>() != null && value?.GetValue<string>() is { } sv && sv.Contains('•')) continue;
            try
            {
                var v = value == null ? null : value.Deserialize(prop.PropertyType, Json.Options);
                if (v is string s) v = s.Trim().Length == 0 && Nullable(prop) ? null : s.Trim();
                if (v == null && !Nullable(prop)) continue;
                prop.SetValue(entity, v);
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
            {
                throw new AppException("errors.invalidField", 400, new { field = JsonNamingPolicy.CamelCase.ConvertName(prop.Name) });
            }
        }
    }

    private static readonly NullabilityInfoContext Nullability = new();
    private static bool Nullable(PropertyInfo p) =>
        !p.PropertyType.IsValueType ? Nullability.Create(p).WriteState == NullabilityState.Nullable
                                    : System.Nullable.GetUnderlyingType(p.PropertyType) != null;

    private async Task<IActionResult> DeleteCore<T>(ResourceDef d0, string id, RecordStatus to) where T : Entity, new()
    {
        var d = (ResourceDef<T>)d0;
        if (d.ReadOnly) throw new AppException("errors.readOnly", 405);
        var entity = await db.Set<T>().FirstOrDefaultAsync(x => x.Id == id) ?? throw new AppException("errors.notFound", 404);
        var scope = await ScopeAsync();
        var related = d.Related?.Invoke(scope).Compile()(entity) ?? false;
        var need = to == RecordStatus.Active ? PermAction.Manage : d.DeleteNeeds;
        if (!cu.GrantFor(d.Module).Allows(need, related)) throw new AppException("errors.forbidden", 403);
        if (entity is Account acc && to == RecordStatus.Deleted) await Finance.FinanceRules.BeforeAccountDeleteAsync(db, acc);
        entity.RecordStatus = to;
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    private static Expression<Func<T, bool>> EqualsExpr<T>(PropertyInfo prop, string raw)
    {
        var x = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(x, prop);
        var type = System.Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        object? value = raw is "null" or "" ? null
            : type.IsEnum ? Enum.Parse(type, raw, true)
            : type == typeof(string) ? raw
            : type == typeof(DateOnly) ? DateOnly.Parse(raw)
            : type == typeof(bool) ? raw is "1" or "true"
            : Convert.ChangeType(raw, type, System.Globalization.CultureInfo.InvariantCulture);
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(member, Expression.Constant(value, prop.PropertyType)), x);
    }

    private static Expression<Func<T, bool>> SearchExpr<T>(string[] fields, string text)
    {
        var x = Expression.Parameter(typeof(T), "x");
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        Expression? any = null;
        foreach (var f in fields)
        {
            var m = Expression.Property(x, f);
            var test = Expression.AndAlso(Expression.NotEqual(m, Expression.Constant(null, typeof(string))),
                Expression.Call(m, contains, Expression.Constant(text)));
            any = any == null ? test : Expression.OrElse(any, test);
        }
        return Expression.Lambda<Func<T, bool>>(any!, x);
    }

    private static IQueryable<T> OrderExpr<T>(IQueryable<T> q, string prop, bool desc)
    {
        var x = Expression.Parameter(typeof(T), "x");
        var body = Expression.Property(x, prop);
        var lambda = Expression.Lambda(body, x);
        var method = desc ? "OrderByDescending" : "OrderBy";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), body.Type], q.Expression, Expression.Quote(lambda));
        return q.Provider.CreateQuery<T>(call);
    }
}
