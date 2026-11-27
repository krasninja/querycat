using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Functions;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Functions;

/// <summary>
/// Tests for <see cref="MiscFunctions" />.
/// </summary>
public sealed class MiscFunctionsTests
{
    private readonly ExecutionThreadBootstrapper _executionThreadBootstrapper = TestThread.CreateBootstrapper();

    [Fact]
    public async Task Coalesce_SeveralArgs_ShouldReturnFirstNotNull()
    {
        // Act.
        await using var thread = _executionThreadBootstrapper.Create();
        var result1 = await thread.RunAsync(@"ECHO COALESCE(NULL, 10);", cancellationToken: TestContext.Current.CancellationToken);
        var result2 = await thread.RunAsync(@"ECHO COALESCE(NULL, 10, 20);", cancellationToken: TestContext.Current.CancellationToken);
        var result3 = await thread.RunAsync(@"ECHO COALESCE(NULL, 10 + NULL, NULL);", cancellationToken: TestContext.Current.CancellationToken);

        // Assert.
        Assert.Equal(10, result1.AsInteger);
        Assert.Equal(10, result2.AsInteger);
        Assert.True(result3.IsNull);
    }
}
