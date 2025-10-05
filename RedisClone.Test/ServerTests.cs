using System.Net.Sockets;
using System.Text;

namespace RedisClone.Test;

public class ServerTests
{
    private const int Port = 63970;
    private const string Host = "127.0.0.1";

    [Fact]
    public async Task Ping_Command_ReturnsPong()
    {
        // Arrange: start server
        var server = new Server(Host, 63970);
        _ = server.StartAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(Host, Port, TestContext.Current.CancellationToken);

        using var stream = client.GetStream();

        // Act: send PING
        var request = "*1\r\n$4\r\nPING\r\n";
        var requestBytes = Encoding.UTF8.GetBytes(request);
        await stream.WriteAsync(requestBytes, TestContext.Current.CancellationToken);

        // Assert: read response
        var buffer = new byte[256];
        int read = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);
        var response = Encoding.UTF8.GetString(buffer, 0, read);

        Assert.Equal("+PONG\r\n", response);
    }

}
