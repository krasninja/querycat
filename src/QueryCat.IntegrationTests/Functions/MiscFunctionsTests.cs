using Xunit;
using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.IntegrationTests.Functions;

/// <summary>
/// Tests for <see cref="MiscFunctions" />.
/// </summary>
public class MiscFunctionsTests : BaseTests
{
    [Fact]
    public void Coalesce_SeveralArgs_ShouldReturnFirstNotNull()
    {
        // Act.
        var result1 = Runner.Run(@"ECHO COALESCE(NULL, 10);");
        var result2 = Runner.Run(@"ECHO COALESCE(NULL, 10, 20);");
        var result3 = Runner.Run(@"ECHO COALESCE(NULL, 10 + NULL, NULL);");

        // Assert.
        Assert.Equal(10, result1.AsInteger);
        Assert.Equal(10, result2.AsInteger);
        Assert.True(result3.IsNull);
    }
}
