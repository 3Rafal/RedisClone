namespace RedisClone.Test;

public class KeyValueStoreTests
{
    [Fact]
    public void TryGet_ReturnsValue_AfterSet()
    {
        var store = new KeyValueStore();

        store.Set("foo", "bar");

        var found = store.TryGet("foo", out var value);

        Assert.True(found);
        Assert.Equal("bar", value);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyMissing()
    {
        var store = new KeyValueStore();

        var found = store.TryGet("missing", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public async Task TryGet_ReturnsFalse_WhenValueExpired()
    {
        var store = new KeyValueStore();
        store.Set("temp", "value", pxMs: 50);

        await Task.Delay(80, TestContext.Current.CancellationToken);

        var found = store.TryGet("temp", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var store = new KeyValueStore();
        store.Set("key", "value1");
        store.Set("key", "value2");

        var found = store.TryGet("key", out var value);

        Assert.True(found);
        Assert.Equal("value2", value);
    }

    [Fact]
    public void Del_RemovesSpecifiedKeys()
    {
        var store = new KeyValueStore();
        store.Set("a", "1");
        store.Set("b", "2");
        store.Set("c", "3");

        var removed = store.Del(["a", "c", "missing"]);

        Assert.Equal(2, removed);
        Assert.False(store.TryGet("a", out _));
        Assert.False(store.TryGet("c", out _));
        Assert.True(store.TryGet("b", out _));
    }

    [Fact]
    public async Task Exists_CountsOnlyPresentKeys()
    {
        var store = new KeyValueStore();
        store.Set("alive", "1");
        store.Set("short", "1", pxMs: 50);

        await Task.Delay(80, TestContext.Current.CancellationToken);

        var count = store.Exists(["alive", "short", "missing"]);

        Assert.Equal(1, count);
    }

    [Fact]
    public void Expire_SetsExpirationAndReturnsOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");

        var updated = store.Expire("key", seconds: 2);
        var ttl = store.Ttl("key");

        Assert.Equal(1, updated);
        Assert.InRange(ttl, 1, 2);
    }

    [Fact]
    public async Task Expire_RemovesExpiredEntry()
    {
        var store = new KeyValueStore();
        store.Set("temp", "value");
        store.Expire("temp", seconds: 0);

        await Task.Delay(20, TestContext.Current.CancellationToken);

        var ttl = store.Ttl("temp");

        Assert.Equal(-2, ttl);
    }

    [Fact]
    public async Task Persist_RemovesExistingExpiration()
    {
        var store = new KeyValueStore();
        store.Set("key", "value", pxMs: 200);

        var ttlBefore = store.Ttl("key");
        var result = store.Persist("key");
        await Task.Delay(250, TestContext.Current.CancellationToken);
        var ttlAfter = store.Ttl("key");

        Assert.True(ttlBefore > 0);
        Assert.Equal(1, result);
        Assert.Equal(-1, ttlAfter);
    }

    [Fact]
    public void Persist_ReturnsZero_WhenNoExpiration()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");

        var result = store.Persist("key");

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Ttl_ReturnsMinusTwo_WhenKeyMissingOrExpired()
    {
        var store = new KeyValueStore();
        store.Set("temp", "value", pxMs: 50);

        var missingTtl = store.Ttl("missing");
        await Task.Delay(80, TestContext.Current.CancellationToken);
        var expiredTtl = store.Ttl("temp");

        Assert.Equal(-2, missingTtl);
        Assert.Equal(-2, expiredTtl);
    }

    [Fact]
    public async Task PurgeExpiredNow_RemovesExpiredEntries()
    {
        var store = new KeyValueStore();
        store.Set("expired", "1", pxMs: 10);
        store.Set("alive", "2");

        await Task.Delay(30, TestContext.Current.CancellationToken);
        store.PurgeExpiredNow();

        Assert.False(store.TryGet("expired", out _));
        Assert.True(store.TryGet("alive", out _));
    }
}
