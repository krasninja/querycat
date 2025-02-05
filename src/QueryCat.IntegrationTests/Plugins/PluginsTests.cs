using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Functions;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public sealed class PluginsTests
{
    private readonly ExecutionThreadBootstrapper _executionThreadBootstrapper = TestThread.CreateBootstrapper();

    [Fact]
    public async Task SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        await thread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(thread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public async Task SamplePluginRowsIterator_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.FunctionsManager.RegisterFunction(SamplePluginRowsIterator.SamplePlugin);

        // Act.
        await thread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(thread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public async Task SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.FunctionsManager.RegisterFunction(SamplePluginInput.SamplePlugin);

        // Act.
        await thread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(thread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    private static string PrepareResult(string result)
    {
        return result.Replace("\n", string.Empty);
    }
}
