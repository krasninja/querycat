using Xunit;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public sealed class PluginsTests : IDisposable
{
    private readonly TestThread _testThread = new();

    [Fact]
    public void SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testThread.GetQueryResult());

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
        var result = PrepareResult(_testThread.GetQueryResult());

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
        var result = PrepareResult(_testThread.GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testThread.FunctionsManager.RegisterFunction(SamplePluginEnumerableInput.SamplePlugin);

        // Act.
        _testThread.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testThread.GetQueryResult());

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
