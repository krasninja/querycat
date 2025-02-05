using QueryCat.Backend;
using Xunit;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Tests.QueryRunner;

namespace QueryCat.IntegrationTests.Execution;

/// <summary>
/// Tests for <see cref="DefaultExecutionThread" />.
/// </summary>
public class DefaultExecutionThreadTests
{
    private readonly ExecutionThreadBootstrapper _executionThreadBootstrapper = TestThread.CreateBootstrapper()
        .WithRegistrations(AdditionalRegistration.Register);

    [Fact]
    public async Task Run_WithParameters_ShouldProcess()
    {
        // Arrange.
        using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.TopScope.Variables["param1"] = new(11);
        var @params = new Dictionary<string, VariantValue>
        {
            ["param2"] = new(24),
            ["param3"] = new(42),
        };

        // Act.
        var result = await thread.RunAsync("param1 + param2 + param3;", @params);

        // Assert.
        Assert.Equal(77, result.AsInteger);
        Assert.False(thread.TopScope.Variables.ContainsKey("param2"));
    }

    [Fact]
    public async Task Run_WithParameterNestedScope_TopScopeShouldOverride()
    {
        // Arrange.
        using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.TopScope.Variables["param1"] = new(2023);
        var @params = new Dictionary<string, VariantValue>
        {
            ["param1"] = new(2024),
        };

        // Act.
        var result = await thread.RunAsync("param1;", @params);

        // Assert.
        Assert.Equal(2024, result.AsInteger);
    }
}
