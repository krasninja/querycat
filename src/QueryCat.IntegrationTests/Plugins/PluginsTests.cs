using Xunit;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public sealed class PluginsTests : IDisposable
{
    private readonly IExecutionThread _testThread;

    public PluginsTests()
    {
        _testThread = TestThread.CreateBootstrapper()
            .Create();
    }

    [Fact]
    public void SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginClassRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginClassRowsInput.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginRowsIterator_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginRowsIterator.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(TestThread.GetQueryResult(_testThread));

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginInput.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
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
