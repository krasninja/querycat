using System.Buffers;
using System.Globalization;
using System.Text;
using Xunit;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="DelimiterStreamReader" />.
/// </summary>
public class DelimiterStreamReaderTests
{
    [Fact]
    public async Task ReadAsync_CsvWithWindowsNewLines_ShouldParseCorrect()
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
    public async Task ReadAsync_CsvWithUnixNewLines_ShouldParseCorrect()
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
    public async Task ReadAsync_CsvTextWithQuotes_ShouldUnquote()
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
    public async Task ReadAsync_QuotesAtTheEnd_ShouldUnquote()
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
    public async Task ReadAsync_LastFieldWithQuotes_ShouldParse()
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
    public async Task ReadAsync_LastFieldEmpty_ShouldGetField()
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
    public async Task ReadAsync_MultipleQuoteStrings_ShouldUnquote()
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
    public async Task ReadAsync_TextFromStdin_ShouldParse()
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
    public async Task ReadAsync_LogTextFromStdin_ShouldParse()
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
    public async Task ReadAsync_DataWithEmptyLines_ShouldSkipEmpty()
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
    public async Task ReadAsync_DataWithEmptyFields_ShouldParse()
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
    public async Task ReadAsync_VariableDynamicBufferLength_ShouldParse()
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

    [Fact]
    public async Task ReadAsync_BufferSize5_ShouldAvoidForeverLoop()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(StringToStream("a0,bb0,ccc0\na1,bb1,ccc1\n"), new DelimiterStreamReader.ReaderOptions()
        {
            BufferSize = 5,
            Delimiters = [','],
        });

        // Act.
        var b1 = await streamRowsInput.ReadAsync();
        var b2 = await streamRowsInput.ReadAsync();
        var b3 = await streamRowsInput.ReadAsync();
        var b4 = await streamRowsInput.ReadAsync();

        // Assert.
        Assert.True(b1);
        Assert.True(b2);
        Assert.False(b3);
        Assert.False(b4);
    }

    [Fact]
    public async Task ReadLineAsync_CommentWithComma_ShouldAvoidForeverLoop()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(StringToStream("id,name\n//com,ment\n10,john\n"), new DelimiterStreamReader.ReaderOptions()
        {
            Delimiters = [','],
        });

        // Act.
        var b1 = await streamRowsInput.ReadAsync();
        var b2 = await streamRowsInput.ReadLineAsync();

        // Assert.
        Assert.True(b1);
        Assert.True(b2);
        Assert.Equal("//com,ment", streamRowsInput.GetString(0));
    }

    [Fact]
    public async Task ReadAsync_TryDetectDelimiterNoEol_ShouldDetectComma()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(
            StringToStream("a,b,c"), new DelimiterStreamReader.ReaderOptions
            {
                DetectDelimiter = true,
            });

        // Act.
        var b1 = await streamRowsInput.ReadAsync();

        // Assert.
        Assert.True(b1);
        Assert.Equal("c", streamRowsInput.GetField(2));
    }

    [Fact]
    public async Task ReadAsync_1CharQuote_ShouldNotCrash()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(
            StringToStream("a,\""), new DelimiterStreamReader.ReaderOptions
            {
                QuoteChars = ['"'],
                DetectDelimiter = true,
            });

        // Act.
        var b1 = await streamRowsInput.ReadAsync();

        // Assert.
        Assert.True(b1);
        Assert.Equal(string.Empty, streamRowsInput.GetField(1));
    }

    [Fact]
    public async Task ReadAsync_UnterminatedQuoteAtEof_ShouldNotDropLastChar()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(
            StringToStream("a,\"bc"), new DelimiterStreamReader.ReaderOptions
            {
                QuoteChars = ['"'],
            });

        // Act.
        await streamRowsInput.ReadAsync();

        // Assert.
        Assert.Equal("bc", streamRowsInput.GetField(1));
    }

    [Fact]
    public async Task ReadAsync_BufferSize4_ShouldUnquote()
    {
        // Arrange.
        var streamRowsInput = new DelimiterStreamReader(
            StringToStream("abc,\"d\"\n"), new DelimiterStreamReader.ReaderOptions
        {
            BufferSize = 4,
            QuoteChars = ['"'],
            Delimiters = [','],
        });

        // Act.
        var b1 = await streamRowsInput.ReadAsync();

        // Assert.
        Assert.True(b1);
        Assert.Equal("abc", streamRowsInput.GetField(0).ToString());
        Assert.Equal("d", streamRowsInput.GetField(1).ToString());
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
