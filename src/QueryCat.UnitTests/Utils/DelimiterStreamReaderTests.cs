using Xunit;
using System.Text;
using QueryCat.Backend.Utils;

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
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ',' },
            });
        streamRowsInput.Read();
        streamRowsInput.Read();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public void Read_CsvWithUnixNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\n")
            .Append("10,john\n");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ',' },
            });
        streamRowsInput.Read();
        streamRowsInput.Read();

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
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ',' },
            });
        streamRowsInput.Read();
        streamRowsInput.ReadLine();

        // Assert.
        Assert.Equal("//comment", streamRowsInput.GetField(0).ToString());
    }

    [Fact]
    public void Read_CsvTextWithQuotes_ShouldUnquote()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("no quotes,\"has quotes\",\"inner\"\"quote\"\"s\",'mixed\"quotes'");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ',' },
                QuoteChars = new[] { '"', '\'' },
            });
        streamRowsInput.Read();

        // Assert.
        Assert.Equal("no quotes", streamRowsInput.GetField(0).ToString());
        Assert.Equal("has quotes", streamRowsInput.GetField(1).ToString());
        Assert.Equal("inner\"quote\"s", streamRowsInput.GetField(2).ToString());
        Assert.Equal("mixed\"quotes", streamRowsInput.GetField(3).ToString());
    }

    [Fact]
    public void Read_TextFromStdin_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append(" 10     explorer\n")
            .Append(" 5323  \"quake 2\"");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ' ' },
                QuoteChars = new[] { '"' },
                DelimitersCanRepeat = true,
            });
        streamRowsInput.Read();
        var id1 = streamRowsInput.GetField(0).ToString();
        var name1 = streamRowsInput.GetField(1).ToString();
        streamRowsInput.Read();
        var id2 = streamRowsInput.GetField(0).ToString();
        var name2 = streamRowsInput.GetField(1).ToString();

        // Assert.
        Assert.Equal("10", id1);
        Assert.Equal("5323", id2);
        Assert.Equal("explorer", name1);
        Assert.Equal("quake 2", name2);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("\"test\"", "test")]
    [InlineData("\"test with \"\"quote\"\"\"", "test with \"quote\"")]
    [InlineData("test with \"\"quote\"\"", "test with \"quote\"")]
    public void Unquote(string target, string expected)
    {
        // Act.
        var result1 = DelimiterStreamReader.Unquote(target).ToString();

        // Assert.
        Assert.Equal(expected, result1);
    }

    private static StreamReader StringToStream(string value)
        => new(new MemoryStream(Encoding.UTF8.GetBytes(value)));
}
