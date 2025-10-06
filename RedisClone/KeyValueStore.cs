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

    public long Exists(IEnumerable<string> keys)
    {
        long count = 0;
        foreach (var k in keys)
            if (TryGet(k, out _)) count++;

        return count;
    }

    // TODO: NX | XX | GT | LT
    public long Expire(string key, long seconds)
    {
        if (!_map.TryGetValue(key, out var entry)) return 0;

        if (entry.ExpireAtMs is long ts && ts <= NowMs)
        {
            _map.TryRemove(key, out _);
            return 0;
        }

        entry.ExpireAtMs = NowMs + seconds * 1000;
        return 1;
    }

    public long Persist(string key)
    {
        if (!_map.TryGetValue(key, out var entry)) return 0;

        if (entry.ExpireAtMs is null) return 0;

        entry.ExpireAtMs = null;
        return 1;
    }

    public long Ttl(string key)
    {
        if (!_map.TryGetValue(key, out var entry)) return -2;
        if (entry.ExpireAtMs is null) return -1;

        long msLeft = entry.ExpireAtMs.Value - NowMs;
        if (msLeft <= 0)
        {
            _map.TryRemove(key, out _);
            return -2;
        }

        return (long)Math.Ceiling(msLeft / 1000.0);
    }

    public void PurgeExpiredNow()
    {
        foreach (var kv in _map)
        {
            var e = kv.Value;
            if (e.ExpireAtMs is long ts && ts <= NowMs)
                _map.TryRemove(kv.Key, out _);
        }
    }
}
