using System.Collections.Concurrent;

namespace RedisClone;

public class ValueEntry
{
    public string Value { get; init; } = "";
    public long? ExpireAtMs { get; set; }
}

public class KeyValueStore
{
    private readonly ConcurrentDictionary<string, ValueEntry> _map =
        new(StringComparer.Ordinal);

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public bool TryGet(string key, out string? value)
    {
        value = null;

        if (!_map.TryGetValue(key, out var entry))
            return false;

        if (entry.ExpireAtMs is long ts && ts <= NowMs)
        {
            _map.TryRemove(key, out _);
            return false;
        }

        value = entry.Value;
        return true;
    }

    public void Set(string key, string value, long? pxMs = null)
    {
        var expireAt = pxMs.HasValue ? NowMs + pxMs.Value : (long?)null;
        _map[key] = new ValueEntry { Value = value, ExpireAtMs = expireAt };
    }

    public long Del(IEnumerable<string> keys)
    {
        long removed = 0;

        foreach (var k in keys)
            if (_map.TryRemove(k, out _)) removed++;

        return removed;
    }
}
