using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public sealed class PluginsTests : IDisposable
{
    private readonly IExecutionThread<ExecutionOptions> _testThread;

    public PluginsTests()
    {
        _testThread = TestThread.CreateBootstrapper()
            .CreateAsync()
            .GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        await _testThread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public async Task SamplePluginRowsIterator_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginRowsIterator.SamplePlugin);

        // Act.
        await _testThread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public async Task SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginInput.SamplePlugin);

        // Act.
        await _testThread.RunAsync(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    private static string PrepareResult(string result)
    {
        return result.Replace("\n", string.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _testThread.Dispose();
    }
}
