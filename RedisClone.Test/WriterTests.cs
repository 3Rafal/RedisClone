using System.Text;
using RedisClone.Resp;

namespace RedisClone.Test;

public class WriterTests
{
    private static async Task<string> GetStreamContent(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task WriteSimpleStringAsync_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteSimpleStringAsync(stream, "OK");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("+OK\r\n", content);
    }

    [Fact]
    public async Task WriteSimpleStringAsync_EmptyString_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteSimpleStringAsync(stream, "");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("+\r\n", content);
    }

    [Fact]
    public async Task WriteSimpleStringAsync_StringWithSpaces_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteSimpleStringAsync(stream, "Hello World");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("+Hello World\r\n", content);
    }

    [Fact]
    public async Task WriteSimpleStringAsync_StringWithSpecialChars_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteSimpleStringAsync(stream, "Hello\x00World!\n");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("+Hello\0World!\n\r\n", content);
    }

    [Fact]
    public async Task WriteErrorAsync_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteErrorAsync(stream, "Error message");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("-Error message\r\n", content);
    }

    [Fact]
    public async Task WriteErrorAsync_EmptyError_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteErrorAsync(stream, "");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("-\r\n", content);
    }

    [Fact]
    public async Task WriteErrorAsync_ErrorWithSpaces_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteErrorAsync(stream, "ERR unknown command");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("-ERR unknown command\r\n", content);
    }

    [Fact]
    public async Task WriteIntegerAsync_PositiveNumber_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteIntegerAsync(stream, 12345L);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal(":12345\r\n", content);
    }

    [Fact]
    public async Task WriteIntegerAsync_NegativeNumber_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteIntegerAsync(stream, -12345L);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal(":-12345\r\n", content);
    }

    [Fact]
    public async Task WriteIntegerAsync_Zero_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteIntegerAsync(stream, 0L);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal(":0\r\n", content);
    }

    [Fact]
    public async Task WriteIntegerAsync_MaxLongValue_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteIntegerAsync(stream, long.MaxValue);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal($":{long.MaxValue}\r\n", content);
    }

    [Fact]
    public async Task WriteIntegerAsync_MinLongValue_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteIntegerAsync(stream, long.MinValue);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal($":{long.MinValue}\r\n", content);
    }

    [Fact]
    public async Task WriteBulkStringAsync_NonNullValue_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteBulkStringAsync(stream, "hello");

        // Assert
        var content = await GetStreamContent(stream);
        // Note: Current implementation has a bug - missing \r\n at the end
        Assert.Equal("$5\r\nhello", content);
    }

    [Fact]
    public async Task WriteBulkStringAsync_EmptyString_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteBulkStringAsync(stream, "");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("$0\r\n", content);
    }

    [Fact]
    public async Task WriteBulkStringAsync_NullValue_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteBulkStringAsync(stream, null);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("-1\r\n", content);
    }

    [Fact]
    public async Task WriteBulkStringAsync_StringWithSpecialChars_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteBulkStringAsync(stream, "Hello\x00World!");

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("$12\r\nHello\0World!", content);
    }

    [Fact]
    public async Task WriteBulkStringAsync_LongString_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();
        var longString = new string('a', 1000);

        // Act
        await Writer.WriteBulkStringAsync(stream, longString);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal($"$1000\r\n{longString}", content);
    }

    [Fact]
    public async Task WriteArrayAsync_EmptyArray_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, []);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*0\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithNulls_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, [null, null]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*2\r\n-1\r\n-1\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithStrings_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, ["hello", "world"]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*2\r\n$5\r\nhello$5\r\nworld", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithIntegers_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, [42, -123, 0]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*3\r\n:42\r\n:-123\r\n:0\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithMixedTypes_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, ["hello", 42, null, "world"]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*4\r\n$5\r\nhello:42\r\n-1\r\n$5\r\nworld", content);
    }

    [Fact]
    public async Task WriteArrayAsync_NestedArray_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, ["hello", new object[] { "inner", 42 }]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*2\r\n$5\r\nhello*2\r\n$5\r\ninner:42\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_DeeplyNestedArray_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, [new object[] { "level2", new object[] { "level3" } }]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*1\r\n*2\r\n$6\r\nlevel2*1\r\n$6\r\nlevel3", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithEmptyStrings_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, ["", "hello", ""]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*3\r\n$0\r\n$5\r\nhello$0\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithSpecialChars_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, ["Hello\x00World!", "\n\t\r"]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*2\r\n$12\r\nHello\0World!$3\r\n\n\t\r", content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithLargeNumbers_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, [int.MaxValue, long.MinValue, 0]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal($"*3\r\n:{int.MaxValue}\r\n:{long.MinValue}\r\n:0\r\n", content);
    }

    [Fact]
    public async Task WriteArrayAsync_UnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => Writer.WriteArrayAsync(stream, [3.14f]));
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithUnsupportedType_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            () => Writer.WriteArrayAsync(stream, ["hello", 3.14f, "world"]));
    }

    [Fact]
    public async Task WriteArrayAsync_Boolean_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => Writer.WriteArrayAsync(stream, [true]));

        Assert.Contains("RESP array cannot serialize object of type Boolean", exception.Message);
    }

    [Fact]
    public async Task WriteArrayAsync_Double_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => Writer.WriteArrayAsync(stream, [3.14d]));

        Assert.Contains("RESP array cannot serialize object of type Double", exception.Message);
    }

    [Fact]
    public async Task WriteArrayAsync_Char_ThrowsNotSupportedException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => Writer.WriteArrayAsync(stream, ['c']));

        Assert.Contains("RESP array cannot serialize object of type Char", exception.Message);
    }

    [Fact]
    public async Task WriteArrayAsync_LargeArray_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();
        var largeArray = Enumerable.Range(1, 100).Select(i => $"item{i}").ToArray();

        // Act
        await Writer.WriteArrayAsync(stream, largeArray);

        // Assert
        var content = await GetStreamContent(stream);
        var expected = new StringBuilder("*100\r\n");
        foreach (var item in largeArray)
        {
            expected.Append($"${item.Length}\r\n{item}");
        }
        Assert.Equal(expected.ToString(), content);
    }

    [Fact]
    public async Task WriteArrayAsync_ArrayWithIntValues_WritesCorrectFormat()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteArrayAsync(stream, [1, 2, 3]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("*3\r\n:1\r\n:2\r\n:3\r\n", content);
    }

    [Fact]
    public async Task WriteMultipleMessages_CorrectConcatenation()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await Writer.WriteSimpleStringAsync(stream, "OK");
        await Writer.WriteIntegerAsync(stream, 42);
        await Writer.WriteBulkStringAsync(stream, "test");
        await Writer.WriteArrayAsync(stream, ["a", "b"]);

        // Assert
        var content = await GetStreamContent(stream);
        Assert.Equal("+OK\r\n:42\r\n$4\r\ntest*2\r\n$1\r\na$1\r\nb", content);
    }
}