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
    public async Task Read_CsvWithWindowsNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\r\n")
            .Append("10,john");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public async Task Read_CsvWithUnixNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id\tname\n") // len = 8
            .Append("10\tjohn\n"); // len = 8

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public async Task ReadLine_CsvText_ShouldReadWholeLine()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\n")
            .Append("//comment\n")
            .Append("10,john\n");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()));
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadLineAsync();

        // Assert.
        Assert.Equal("//comment", streamRowsInput.GetField(0).ToString());
    }

    [Fact]
    public async Task Read_CsvTextWithQuotes_ShouldUnquote()
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
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("no quotes", streamRowsInput.GetField(0).ToString());
        Assert.Equal("has quotes", streamRowsInput.GetField(1).ToString());
        Assert.Equal("inner\"quote\"s", streamRowsInput.GetField(2).ToString());
        Assert.Equal("mixed\"quotes", streamRowsInput.GetField(3).ToString());
        Assert.Equal("space offset", streamRowsInput.GetField(4).ToString());
    }

    [Fact]
    public async Task Read_QuotesAtTheEnd_ShouldUnquote()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("no quotes,\"has quotes\"\n")
            .Append("1,2");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
                QuoteChars = ['"'],
                Culture = CultureInfo.InvariantCulture,
            });
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal(2, streamRowsInput.GetFieldsCount());
        Assert.Equal("no quotes", streamRowsInput.GetField(0).ToString());
        Assert.Equal("has quotes", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public async Task Read_LastFieldWithQuotes_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("mark \"A\",mark \"B\" here");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
                QuoteChars = ['"'],
                Culture = CultureInfo.InvariantCulture,
            });
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal(2, streamRowsInput.GetFieldsCount());
        Assert.Equal("mark \"A\"", streamRowsInput.GetField(0).ToString());
        Assert.Equal("mark \"B\" here", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public async Task Read_LastFieldEmpty_ShouldGetField()
    {
        // Arrange.
        var sb = new StringBuilder()
            .AppendLine("1,2,")
            .AppendLine("3,4,");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
            });
        await streamRowsInput.ReadAsync();
        var field1 = streamRowsInput.GetField(0).ToString();
        var field2 = streamRowsInput.GetField(1).ToString();
        var fieldCount1 = streamRowsInput.GetFieldsCount();
        await streamRowsInput.ReadAsync();
        var field3 = streamRowsInput.GetField(0).ToString();
        var field4 = streamRowsInput.GetField(1).ToString();
        var fieldCount2 = streamRowsInput.GetFieldsCount();

        // Assert.
        Assert.Equal(3, fieldCount1);
        Assert.Equal("1", field1);
        Assert.Equal("2", field2);
        Assert.Equal(3, fieldCount2);
        Assert.Equal("3", field3);
        Assert.Equal("4", field4);
    }

    [Fact]
    public async Task Read_OneColumnData_ShouldReturn()
    {
        // Arrange.
        var sb = new StringBuilder()
            .AppendLine("id1")
            .AppendLine("id2");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
                Culture = CultureInfo.InvariantCulture,
            });
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal(1, streamRowsInput.GetFieldsCount());
    }

    [Fact]
    public async Task Read_MultipleQuoteStrings_ShouldUnquote()
    {
        // Arrange.
        var sb = new StringBuilder()
            .AppendLine("fox,\"bobr\"  \"dobr\",cat");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()),
            new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = [','],
                QuoteChars = ['"'],
                Culture = CultureInfo.InvariantCulture,
            });
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal(3, streamRowsInput.GetFieldsCount());
        Assert.Equal("fox", streamRowsInput.GetField(0).ToString());
        Assert.Equal("bobr\"  \"dobr", streamRowsInput.GetField(1).ToString());
        Assert.Equal("cat", streamRowsInput.GetField(2).ToString());
    }

    [Fact]
    public async Task Read_TextFromStdin_ShouldParse()
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
        await streamRowsInput.ReadAsync();
        var id1 = streamRowsInput.GetField(0).ToString();
        var name1 = streamRowsInput.GetField(1).ToString();
        await streamRowsInput.ReadAsync();
        var id2 = streamRowsInput.GetField(0).ToString();
        var name2 = streamRowsInput.GetField(1).ToString();

        // Assert.
        Assert.Equal("10", id1);
        Assert.Equal("5323", id2);
        Assert.Equal("explorer", name1);
        Assert.Equal("quake 2", name2);
    }

    [Fact]
    public async Task Read_LogTextFromStdin_ShouldParse()
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
        await streamRowsInput.ReadAsync();
        var name1 = streamRowsInput.GetField(0).ToString();
        await streamRowsInput.ReadAsync();
        var name2 = streamRowsInput.GetField(0).ToString();

        // Assert.
        Assert.Equal("ivan", name1);
        Assert.Equal("affka", name2);
    }

    [Fact]
    public async Task Read_DataWithEmptyLines_ShouldSkipEmpty()
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
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("10", streamRowsInput.GetField(0).ToString());
        Assert.Equal("john", streamRowsInput.GetField(1).ToString());
    }

    [Fact]
    public async Task Read_DataWithEmptyFields_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .AppendLine("id,name,age,category")
            .AppendLine("466,ivan,40,web")
            .AppendLine("999,,,");

        // Act.
        var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()), new DelimiterStreamReader.ReaderOptions
        {
            SkipEmptyLines = true,
            Delimiters = [','],
            Culture = CultureInfo.InvariantCulture,
        });
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("999", streamRowsInput.GetField(0).ToString());
        Assert.Empty(streamRowsInput.GetField(1).ToString());
        Assert.Empty(streamRowsInput.GetField(2).ToString());
        Assert.Empty(streamRowsInput.GetField(3).ToString());
    }

    [Fact]
    public async Task Read_VariableDynamicBufferLength_ShouldParse()
    {
        // Arrange.
        var sb = new StringBuilder()
            .AppendLine("col1,col2,col3,col4,col5")
            .AppendLine("0000,0001,0002,0003,0004")
            .AppendLine("0100,0101,0102,0103,0104")
            .AppendLine("0200,0201,0202,0203,0204");

        // Act.
        for (var bufferSize = 2; bufferSize < 200; bufferSize++)
        {
            var streamRowsInput = new DelimiterStreamReader(StringToStream(sb.ToString()), new DelimiterStreamReader.ReaderOptions
            {
                SkipEmptyLines = true,
                Delimiters = [','],
                Culture = CultureInfo.InvariantCulture,
                BufferSize = bufferSize,
            });

            for (var i = 0; i < 4; i++)
            {
                await streamRowsInput.ReadAsync();

                // Assert.
                Assert.Equal(4, streamRowsInput.GetField(0).ToString().Length);
                Assert.Equal(4, streamRowsInput.GetField(1).ToString().Length);
                Assert.Equal(4, streamRowsInput.GetField(2).ToString().Length);
                Assert.Equal(4, streamRowsInput.GetField(3).ToString().Length);
                Assert.Equal(4, streamRowsInput.GetField(4).ToString().Length);
            }
        }
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
