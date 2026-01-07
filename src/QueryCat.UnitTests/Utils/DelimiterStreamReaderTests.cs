using System.Buffers;
using System.Globalization;
using Xunit;
using System.Text;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="DelimiterStreamReader" />.
/// </summary>
public class DelimiterStreamReaderTests
{
    [Fact]
    public void Read_CsvWithWindowsNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\r\n")
            .Append("10,john");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        streamRowsInput.ReadAsync();
        streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public void Read_CsvWithUnixNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id\tname\n") // len = 8
            .Append("10\tjohn\n"); // len = 8

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        streamRowsInput.ReadAsync();
        streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public void ReadLine_CsvText_ShouldReadWholeLine()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\n")
            .Append("//comment\n")
            .Append("10,john\n");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        streamRowsInput.ReadAsync();
        streamRowsInput.ReadLineAsync();

        // Assert.
        Assert.Equal("//comment", streamRowsInput.GetField(0).ToString());
    }

    [Fact]
    public void Read_CsvTextWithQuotes_ShouldUnquote()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("no quotes,\"has quotes\",\"inner\"\"quote\"\"s\",'mixed\"quotes',    \"space offset\"");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
                QuoteChars = ['"', '\''],
                Culture = CultureInfo.InvariantCulture,
            });
        streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("no quotes", streamRowsInput.GetField(0).ToString());
        Assert.Equal("has quotes", streamRowsInput.GetField(1).ToString());
        Assert.Equal("inner\"quote\"s", streamRowsInput.GetField(2).ToString());
        Assert.Equal("mixed\"quotes", streamRowsInput.GetField(3).ToString());
        Assert.Equal("space offset", streamRowsInput.GetField(4).ToString());
    }

    [Fact]
    public void Read_TextFromStdin_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append(" 10       explorer\n")
            .Append(" 5323    \"quake 2\"");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [' '],
                QuoteChars = ['"'],
                SkipRepeatedDelimiters = true,
                Culture = CultureInfo.InvariantCulture,
            });
        streamRowsInput.ReadAsync();
        var id1 = streamRowsInput.GetField(0).ToString();
        var name1 = streamRowsInput.GetField(1).ToString();
        streamRowsInput.ReadAsync();
        var id2 = streamRowsInput.GetField(0).ToString();
        var name2 = streamRowsInput.GetField(1).ToString();

        // Assert.
        Assert.Equal("10", id1);
        Assert.Equal("5323", id2);
        Assert.Equal("explorer", name1);
        Assert.Equal("quake 2", name2);
    }

    [Fact]
    public void Read_LogTextFromStdin_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("ivan     tty1         2022-10-15\n")
            .Append("affka    tty1         2022-10-15\n");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [' '],
                QuoteChars = [],
                SkipRepeatedDelimiters = true,
                Culture = CultureInfo.InvariantCulture,
            });
        streamRowsInput.ReadAsync();
        var name1 = streamRowsInput.GetField(0).ToString();
        streamRowsInput.ReadAsync();
        var name2 = streamRowsInput.GetField(0).ToString();

        // Assert.
        Assert.Equal("ivan", name1);
        Assert.Equal("affka", name2);
    }

    [Fact]
    public void Read_DataWithEmptyLines_ShouldSkipEmpty()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\r\n")
            .Append("\r\n")
            .Append("10,john");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()), new DelimiterStreamReader.ReaderOptions
        {
            SkipEmptyLines = true,
            Delimiters = [','],
            Culture = CultureInfo.InvariantCulture,
        });
        streamRowsInput.ReadAsync();
        streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("\"test\"", "test")]
    [InlineData("\"\"\"test\"\"\"", "\"test\"")]
    [InlineData("\"test with \"\"quote\"\"\"", "test with \"quote\"")]
    [InlineData("test with \"\"quote\"\"", "test with \"quote\"")]
    public void UnquoteDoubleQuotes(string target, string expected)
    {
        // Act.
        var result1 = DelimiterStreamReader.UnquoteDoubleQuotes(
            new ReadOnlySequence<char>(target.AsMemory())).ToString();

        // Assert.
        Assert.Equal(expected, result1);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("te\\\"st", "te\"st")]
    [InlineData("\\\"st\\\'", "\"st'")]
    public void UnquoteBackslash(string target, string expected)
    {
        // Act.
        var result1 = DelimiterStreamReader.UnquoteBackslash(
            new ReadOnlySequence<char>(target.AsMemory())).ToString();

        // Assert.
        Assert.Equal(expected, result1);
    }

    [Theory]
    [InlineData("id,name", ',')]
    [InlineData("id\tfull name\tmiddle name\tbe;be", '\t')]
    [InlineData("name", ' ')]
    public void TryDetectDelimiter(string target, char expected)
    {
        // Act.
        DelimiterStreamReader.TryDetectDelimiter(target, out var delimiter);

        // Assert.
        Assert.Equal(expected, delimiter);
    }

    private static StreamReader StringToStream(string value)
        => new(new MemoryStream(Encoding.UTF8.GetBytes(value)));
}
