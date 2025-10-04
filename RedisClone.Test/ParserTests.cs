using System.Text;
using RedisClone.Resp;

namespace RedisClone.Test;

public class ParserTests
{
    private Parser CreateParserFromString(string data) => new(new MemoryStream(Encoding.UTF8.GetBytes(data)));

    [Fact]
    public async Task SimpleString_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("+OK\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("OK", result);
    }

    [Fact]
    public async Task Error_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("-Error message\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("Error message", result);
    }

    [Fact]
    public async Task Integer_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString(":12345\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal(12345L, result);
    }

    [Fact]
    public async Task NegativeInteger_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString(":-12345\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal(-12345L, result);
    }

    [Fact]
    public async Task ZeroInteger_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString(":0\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public async Task BulkString_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("$5\r\nhello\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task EmptyBulkString_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("$0\r\n\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task NullBulkString_ReturnsNull()
    {
        // Arrange
        var parser = CreateParserFromString("$-1\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Array_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("*2\r\n+hello\r\n$5\r\nworld\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.IsType<object[]>(result);
        var array = (object[])result!;
        Assert.Equal(2, array.Length);
        Assert.Equal("hello", array[0]);
        Assert.Equal("world", array[1]);
    }

    [Fact]
    public async Task EmptyArray_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("*0\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.IsType<object[]>(result);
        var array = (object[])result!;
        Assert.Empty(array);
    }

    [Fact]
    public async Task MixedTypeArray_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("*3\r\n+simple\r\n:-42\r\n$6\r\nstring\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.IsType<object[]>(result);
        var array = (object[])result!;
        Assert.Equal(3, array.Length);
        Assert.Equal("simple", array[0]);
        Assert.Equal(-42L, array[1]);
        Assert.Equal("string", array[2]);
    }

    [Fact]
    public async Task NestedArray_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("*2\r\n*1\r\n+inner\r\n$6\r\nnested\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.IsType<object[]>(result);
        var array = (object[])result!;
        Assert.Equal(2, array.Length);

        Assert.IsType<object[]>(array[0]);
        var innerArray = (object[])array[0];
        Assert.Single(innerArray);
        Assert.Equal("inner", innerArray[0]);

        Assert.Equal("nested", array[1]);
    }

    [Fact]
    public async Task EmptyStream_ReturnsNull()
    {
        // Arrange
        var parser = CreateParserFromString("");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UnknownType_ThrowsException()
    {
        // Arrange
        var parser = CreateParserFromString("?invalid\r\n");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(parser.ReadAsync);
    }

    [Fact]
    public async Task MultipleMessagesSequentially_ReturnsCorrectValues()
    {
        // Arrange
        var parser = CreateParserFromString("+first\r\n:42\r\n$5\r\nhello\r\n");

        // Act
        var first = await parser.ReadAsync();
        var second = await parser.ReadAsync();
        var third = await parser.ReadAsync();

        // Assert
        Assert.Equal("first", first);
        Assert.Equal(42L, second);
        Assert.Equal("hello", third);
    }

    [Fact]
    public async Task SimpleStringWithSpaces_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("+Hello World\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task ErrorWithSpaces_ReturnsCorrectValue()
    {
        // Arrange
        var parser = CreateParserFromString("-ERR unknown command\r\n");

        // Act
        var result = await parser.ReadAsync();

        // Assert
        Assert.Equal("ERR unknown command", result);
    }
}