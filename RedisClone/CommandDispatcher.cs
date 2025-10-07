using RedisClone.Resp;

namespace RedisClone;

public class CommandDispatcher(KeyValueStore store)
{
    private readonly KeyValueStore _store = store;

    public async Task DispatchAsync(Stream s, string[] args)
    {
        if (args.Length == 0)
        {
            await Writer.ErrorAsync(s, "ERR empty command");
            return;
        }

        var op = args[0].ToUpperInvariant();

        switch (op)
        {
            case "PING":
                var msg = args.Length > 1 ? args[1] : "PONG";
                await Writer.SimpleStringAsync(s, msg);
                break;

            case "ECHO":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'ECHO' command");
                    break;
                }
                await Writer.BulkStringAsync(s, args[1]);
                break;

            case "GET":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arugments for 'GET' command");
                    break;
                }
                if (_store.TryGet(args[1], out var val))
                    await Writer.BulkStringAsync(s, val);
                else
                    await Writer.BulkStringAsync(s, null);
                break;
        }
    }
}
