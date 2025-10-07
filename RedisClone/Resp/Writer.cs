using System.Text;

namespace RedisClone.Resp;

public class Writer
{
    public static async Task SimpleStringAsync(Stream stream, string message)
    => await Bytes(stream, $"+{message}\r\n");

    public static async Task ErrorAsync(Stream stream, string message)
    => await Bytes(stream, $"-{message}\r\n");


    public static async Task IntegerAsync(Stream stream, long value)
    => await Bytes(stream, $":{value}\r\n");

    public static async Task BulkStringAsync(Stream stream, string? value)
    {
        var message = value == null ?
            "-1\r\n" : $"${value.Length}\r\n{value}\r\n";
        await Bytes(stream, message);
    }

    public static async Task ArrayAsync(Stream stream, object?[] items)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes($"*{items.Length}\r\n"));

        foreach (var item in items)
        {
            switch (item)
            {
                case null:
                    await BulkStringAsync(stream, null);
                    break;
                case string s:
                    await BulkStringAsync(stream, s);
                    break;
                case int i:
                case long l:
                    await IntegerAsync(stream, Convert.ToInt64(item));
                    break;
                case object[] objArr:
                    await ArrayAsync(stream, objArr);
                    break;
                default:
                    throw new NotSupportedException(
                        $"RESP array cannot serialize object of type {item.GetType().Name}");
            }
        }
    }

    private static async Task Bytes(Stream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(data);
    }
}
