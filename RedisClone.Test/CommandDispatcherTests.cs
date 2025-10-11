using System.Text;
using RedisClone;

namespace RedisClone.Test;

public class CommandDispatcherTests
{
    private static async Task<string> ReadStreamAsync(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DispatchAsync_EmptyArgs_WritesError()
    {
        var store = new KeyValueStore();
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, []);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR empty command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PingWithoutArgument_WritesPong()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PING"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("+PONG\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PingWithArgument_EchoesArgument()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PING", "hello"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("+hello\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_EchoWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["ECHO", "one", "two"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'ECHO' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_Echo_WritesBulkString()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["ECHO", "value"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("$5\r\nvalue\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_GetWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["GET"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'GET' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_GetMissingKey_WritesNullBulkString()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["GET", "missing"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_GetExistingKey_WritesValue()
    {
        var store = new KeyValueStore();
        store.Set("foo", "bar");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["GET", "foo"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("$3\r\nbar\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_SetWithValidArgs_WritesOk()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["SET", "key", "value"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("+OK\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_SetWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["SET", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'SET' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_DelExistingKey_ReturnsOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["DEL", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_DelMissingKey_ReturnsZero()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["DEL", "missing"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_DelWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["DEL"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'DEL' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExpireExistingKey_ReturnsOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXPIRE", "key", "60"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExpireMissingKey_ReturnsZero()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXPIRE", "missing", "60"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExpireWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXPIRE", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'EXPIRE' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExistsExistingKey_ReturnsOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXISTS", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExistsMissingKey_ReturnsZero()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXISTS", "missing"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_ExistsWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["EXISTS"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'EXISTS' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_TtlForNonExpiringKey_ReturnsMinusOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["TTL", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":-1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_TtlForExpiringKey_ReturnsPositiveValue()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        store.Expire("key", 60);
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["TTL", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.StartsWith(":", content);
        Assert.EndsWith("\r\n", content);
        Assert.True(content.Length > 3); // At least ":1\r\n"
    }

    [Fact]
    public async Task DispatchAsync_TtlForMissingKey_ReturnsMinusTwo()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["TTL", "missing"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":-2\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_TtlWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["TTL"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'TTL' command\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PersistExpiringKey_ReturnsOne()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        store.Expire("key", 60);
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PERSIST", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":1\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PersistNonExpiringKey_ReturnsZero()
    {
        var store = new KeyValueStore();
        store.Set("key", "value");
        var dispatcher = new CommandDispatcher(store);
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PERSIST", "key"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PersistMissingKey_ReturnsZero()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PERSIST", "missing"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task DispatchAsync_PersistWithInvalidArity_WritesError()
    {
        var dispatcher = new CommandDispatcher(new KeyValueStore());
        await using var stream = new MemoryStream();

        await dispatcher.DispatchAsync(stream, ["PERSIST"]);

        var content = await ReadStreamAsync(stream);
        Assert.Equal("-ERR wrong number of arguments for 'PERSIST' command\r\n", content);
    }
}
