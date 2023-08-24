using Xunit;
using QueryCat.Backend.Core.Utils;

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
}
