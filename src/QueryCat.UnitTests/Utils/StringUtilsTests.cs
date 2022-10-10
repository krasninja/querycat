using System.Buffers;
using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="StringUtils" />.
/// </summary>
public class StringUtilsTests
{
    [Theory]
    [InlineData("%", "", true)]
    [InlineData("_Stuff_.txt_", "1Stuff3.txt4", true)]
    [InlineData("/mnt/data%.log", "/mnt/data1.log", true)]
    public void MatchesToLikePattern(string pattern, string str, bool shouldMatch)
    {
        // Act.
        var result = StringUtils.MatchesToLikePattern(pattern, str);

        // Assert.
        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("\"test\"", "test")]
    [InlineData("\"test with \"\"quote\"\"\"", "test with \"quote\"")]
    [InlineData("test with \"\"quote\"\"", "test with \"quote\"")]
    public void Unquote(string target, string expected)
    {
        // Act.
        var result1 = StringUtils.Unquote(target).ToString();
        var result2 = StringUtils.Unquote(new ReadOnlySequence<char>(target.AsMemory())).ToString();

        // Assert.
        Assert.Equal(expected, result1);
        Assert.Equal(expected, result2);
    }
}
