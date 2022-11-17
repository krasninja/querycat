using Xunit;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public class PluginsTests
{
    private readonly Backend.Tests.TestRunner _testRunner = new();

    [Fact]
    public void SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        _testRunner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testRunner.GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginClassRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginClassRowsInput.SamplePlugin);

        // Act.
        _testRunner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testRunner.GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginRowsIterator_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginRowsIterator.SamplePlugin);

        // Act.
        _testRunner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testRunner.GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        _testRunner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginEnumerableInput.SamplePlugin);

        // Act.
        _testRunner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(_testRunner.GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    private static string PrepareResult(string result)
    {
        return result.Replace("\n", string.Empty);
    }
}
