using Xunit;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="StringUtils" />.
/// </summary>
public class StringUtilsTests
{
    [Fact]
    public void Unescape_OctetString_CorrectCode()
    {
        // Arrange and act.
        var result = StringUtils.Unescape(@"Test \13");

        // Assert.
        Assert.Contains(char.ConvertFromUtf32(11), result);
    }

    [Fact]
    public void Unescape_HexString_CorrectCode()
    {
        // Arrange and act.
        var result = StringUtils.Unescape(@"Test \U0001F939 Test");

        // Assert.
        Assert.Contains(char.ConvertFromUtf32(129337), result);
    }
}
