using System.Text;

namespace RedisClone.Resp;

public class Writer
{
    public static async Task WriteSimpleStringAsync(Stream stream, string message)
    => await WriteBytes(stream, $"+{message}\r\n");

    public static async Task WriteErrorAsync(Stream stream, string message)
    => await WriteBytes(stream, $"-{message}\r\n");


    public static async Task WriteIntegerAsync(Stream stream, long value)
    => await WriteBytes(stream, $":{value}\r\n");

    public static async Task WriteBulkStringAsync(Stream stream, string? value)
    {
        var message = value == null ?
            "-1\r\n" : $"${value.Length}\r\n{value}";
        await WriteBytes(stream, message);
    }

    public static async Task WriteArrayAsync(Stream stream, object[] items)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes($"*{items.Length}\r\n"));

        foreach (var item in items)
        {
            switch (item)
            {
                case null:
                    await WriteBulkStringAsync(stream, null);
                    break;
                case string s:
                    await WriteBulkStringAsync(stream, s);
                    break;
                case int i:
                case long l:
                    await WriteIntegerAsync(stream, Convert.ToInt64(item));
                    break;
                case object[] objArr:
                    await WriteArrayAsync(stream, objArr);
                    break;
                default:
                    throw new NotSupportedException(
                        $"RESP array cannot serialize object of type {item.GetType().Name}");
            }
        }
    }

    private static async Task WriteBytes(Stream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(data);
    }
}
