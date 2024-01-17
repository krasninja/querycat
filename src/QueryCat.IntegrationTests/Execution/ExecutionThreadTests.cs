using Xunit;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Execution;

/// <summary>
/// Tests for <see cref="ExecutionThread" />.
/// </summary>
public class ExecutionThreadTests
{
    private readonly IExecutionThread _testThread;

    public ExecutionThreadTests()
    {
        _testThread = TestThread.CreateBootstrapper()
            .WithRegistrations(AdditionalRegistration.Register)
            .Create();
    }

    [Fact]
    public void Run_WithParameters_ShouldProcess()
    {
        // Arrange.
        _testThread.TopScope.Variables["param1"] = new(11);
        var @params = new Dictionary<string, VariantValue>
        {
            ["param2"] = new(24),
            ["param3"] = new(42),
        };

        // Act.
        var result = _testThread.Run("param1 + param2 + param3;", @params);

        // Assert.
        Assert.Equal(77, result.AsInteger);
        Assert.False(_testThread.TopScope.Variables.ContainsKey("param2"));
    }

    [Fact]
    public void Run_WithParameterNestedScope_TopScopeShouldOverride()
    {
        // Arrange.
        _testThread.TopScope.Variables["param1"] = new(2023);
        var @params = new Dictionary<string, VariantValue>
        {
            ["param1"] = new(2024),
        };

        // Act.
        var result = _testThread.Run("param1;", @params);

        // Assert.
        Assert.Equal(2024, result.AsInteger);
    }
}
