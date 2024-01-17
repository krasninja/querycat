using Xunit;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Functions;

/// <summary>
/// Tests for <see cref="MiscFunctions" />.
/// </summary>
public sealed class MiscFunctionsTests : IDisposable
{
    private readonly IExecutionThread _testThread;

    public MiscFunctionsTests()
    {
        _testThread = TestThread.CreateBootstrapper()
            .Create();
    }

    [Fact]
    public void Coalesce_SeveralArgs_ShouldReturnFirstNotNull()
    {
        // Act.
        var result1 = _testThread.Run(@"ECHO COALESCE(NULL, 10);");
        var result2 = _testThread.Run(@"ECHO COALESCE(NULL, 10, 20);");
        var result3 = _testThread.Run(@"ECHO COALESCE(NULL, 10 + NULL, NULL);");

        // Assert.
        Assert.Equal(10, result1.AsInteger);
        Assert.Equal(10, result2.AsInteger);
        Assert.True(result3.IsNull);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _testThread.Dispose();
    }
}
