using Xunit;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Tests for plugins.
/// </summary>
public class PluginsTests : BaseTests
{
    [Fact]
    public void SamplePluginRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginRowsInput.SamplePlugin);

        // Act.
        Runner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginClassRowsInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginClassRowsInput.SamplePlugin);

        // Act.
        Runner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginRowsIterator_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginRowsIterator.SamplePlugin);

        // Act.
        Runner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    [Fact]
    public void SamplePluginEnumerableInput_CreateAndRun_ReturnsResult()
    {
        // Arrange.
        Runner.ExecutionThread.FunctionsManager.RegisterFunction(SamplePluginEnumerableInput.SamplePlugin);

        // Act.
        Runner.Run(@"SELECT * FROM plugin();");
        var result = PrepareResult(GetQueryResult());

        // Assert.
        Assert.Equal("123456789", result);
    }

    private static string PrepareResult(string result)
    {
        return result.Replace("\n", string.Empty);
    }
}
