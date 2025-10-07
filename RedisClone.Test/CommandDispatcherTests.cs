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
        Assert.Equal("-ERR wrong number of arugments for 'GET' command\r\n", content);
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
}
