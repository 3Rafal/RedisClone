using System.Net;
using System.Net.Sockets;
using RedisClone.Resp;

namespace RedisClone;

public class Server(string host, int port)
{
    private readonly TcpListener _listener = new TcpListener(IPAddress.Parse(host), port);
    private readonly KeyValueStore _store = new();

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("Server started...");

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        var parser = new Parser(stream);

        try
        {
            while (true)
            {
                var msg = await parser.ReadAsync();
                if (msg == null) break;

                if (msg is object[] array && array.Length > 0)
                {
                    string command = array[0].ToString()!.ToUpperInvariant();
                    switch (command)
                    {
                        case "PING":
                            await Writer.SimpleStringAsync(stream, "PONG");
                            break;
                        case "SET":
                            _store.Set(array[1].ToString()!, array[2].ToString()!);
                            await Writer.SimpleStringAsync(stream, "OK");
                            break;
                        case "GET":
                            if (_store.TryGet(array[1].ToString()!, out var value))
                                await Writer.BulkStringAsync(stream, value);
                            else
                                await Writer.BulkStringAsync(stream, null);
                            break;
                        default:
                            await Writer.ErrorAsync(stream, $"Unknown command {command}");
                            break;
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
    }
}
