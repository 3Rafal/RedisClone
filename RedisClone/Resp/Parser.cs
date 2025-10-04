using System.Net.Sockets;
using System.Text;

namespace RedisClone.Resp;

public class Parser(NetworkStream stream)
{
    private readonly StreamReader _reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

    public async Task<object?> ReadAsync()
    {
        int prefix = _reader.Peek();
        if (prefix == -1) return null;

        return (char)prefix switch
        {
            '+' => await ReadSimpleStringAsync(),
            '-' => await ReadErrorAsync(),
            ':' => await ReadIntegerAsync(),
            '$' => await ReadBulkStringAsync(),
            '*' => await ReadArrayAsync(),
            _ => throw new Exception("Unknown RESP type"),
        };
    }

    private async Task<string> ReadSimpleStringAsync()
    {
        await _reader.ReadAsync(new char[1], 0, 1); // consume '+'
        return await _reader.ReadLineAsync() ?? "";
    }

    private async Task<string> ReadErrorAsync()
    {
        await _reader.ReadAsync(new char[1], 0, 1); // consume '-'
        return await _reader.ReadLineAsync() ?? "";
    }

    private async Task<long> ReadIntegerAsync()
    {
        await _reader.ReadAsync(new char[1], 0, 1); // consume ':'
        string line = await _reader.ReadLineAsync() ?? "0";
        return long.Parse(line);
    }

    private async Task<string?> ReadBulkStringAsync()
    {
        await _reader.ReadAsync(new char[1], 0, 1); // consume '$'
        int length = int.Parse(await _reader.ReadLineAsync() ?? "-1");
        if (length == -1) return null;

        char[] buffer = new char[length];
        await _reader.ReadBlockAsync(buffer, 0, length);
        await _reader.ReadLineAsync(); // consume trailing \r\n
        return new string(buffer);
    }

    private async Task<object[]> ReadArrayAsync()
    {
        await _reader.ReadAsync(new char[1], 0, 1); // consume '*'
        int length = int.Parse(await _reader.ReadLineAsync() ?? "0");
        var result = new object[length];
        for (int i = 0; i < length; i++)
            result[i] = await ReadAsync() ?? "";
        return result;
    }
}
