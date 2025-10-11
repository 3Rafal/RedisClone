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
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'GET' command");
                    break;
                }
                if (_store.TryGet(args[1], out var val))
                    await Writer.BulkStringAsync(s, val);
                else
                    await Writer.BulkStringAsync(s, null);
                break;
            
            case "DEL":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'DEL' command");
                    break;
                }
                await Writer.IntegerAsync(s, _store.Del([args[1]]));
                break;
            
            case "SET":
                if (args.Length != 3)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'SET' command");
                    break;
                }
                _store.Set(args[1], args[2]);
                await Writer.SimpleStringAsync(s, "OK");
                break;
            
            case "EXPIRE":
                if (args.Length != 3)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'EXPIRE' command");
                    break;
                }
                await Writer.IntegerAsync(s, _store.Expire(args[1], long.Parse(args[2])));
                break;
            
            case "EXISTS":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'EXISTS' command");
                    break;
                }
                await Writer.IntegerAsync(s, _store.Exists([args[1]]));
                break;
            
            case "TTL":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'TTL' command");
                    break;
                }
                await Writer.IntegerAsync(s, _store.Ttl(args[1]));
                break;
            
            case "PERSIST":
                if (args.Length != 2)
                {
                    await Writer.ErrorAsync(s, "ERR wrong number of arguments for 'PERSIST' command");
                    break;
                }
                await Writer.IntegerAsync(s, _store.Persist(args[1]));
                break;
        }
    }
}
