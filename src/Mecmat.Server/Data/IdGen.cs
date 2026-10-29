using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Mecmat.Domain;
using Mecmat.Services;

namespace Mecmat.Data;

/// <summary>Hands out PFX-YYMMDD-#### at the moment an entity is added, so several new rows can be
/// tracked together and linked before the first save. Counters live in memory — the app is a single
/// process — and are seeded from IdCounter (and existing rows) the first time a prefix is used each day.</summary>
public static class IdGen
{
    private static readonly Dictionary<(string Prefix, string Date), int> Counters = new();
    private static readonly Lock Gate = new();

    public static string Next(DbContext db, Type type)
    {
        var prefix = type.GetCustomAttribute<IdPrefixAttribute>()?.Prefix
                     ?? throw new InvalidOperationException($"{type.Name} has no [IdPrefix].");
        var date = Clock.TodayBangkok().ToString("yyMMdd");
        lock (Gate)
        {
            if (!Counters.TryGetValue((prefix, date), out var n))
            {
                n = db.Set<IdCounter>().AsNoTracking().Where(c => c.Prefix == prefix && c.DateKey == date)
                    .Select(c => (int?)c.LastValue).FirstOrDefault() ?? 0;
            }
            n++;
            Counters[(prefix, date)] = n;
            return $"{prefix}-{date}-{n:0000}";
        }
    }
}

public sealed class EntityIdGenerator : ValueGenerator<string>
{
    public override bool GeneratesTemporaryValues => false;
    public override string Next(EntityEntry entry) => IdGen.Next(entry.Context, entry.Entity.GetType());
}
