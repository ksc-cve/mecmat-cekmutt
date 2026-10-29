using System.Collections.Concurrent;

namespace Mecmat.Services;

/// <summary>Ten failures per IP or per username in five minutes, then 429. In memory on purpose:
/// a restart forgetting the counters is harmless, and it costs nothing on a single-core VPS.</summary>
public sealed class LoginThrottle
{
    private const int Limit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _fails = new();

    public bool Blocked(params string[] keys) => keys.Any(k => Count(k) >= Limit);

    public void Fail(params string[] keys)
    {
        foreach (var k in keys) _fails.GetOrAdd(k, _ => new()).Enqueue(DateTime.UtcNow);
    }

    public void Reset(string key) => _fails.TryRemove(key, out _);

    private int Count(string key)
    {
        if (!_fails.TryGetValue(key, out var q)) return 0;
        while (q.TryPeek(out var t) && DateTime.UtcNow - t > Window) q.TryDequeue(out _);
        return q.Count;
    }
}
