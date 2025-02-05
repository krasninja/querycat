using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="VariablesCompletionSource" />.
/// </summary>
public sealed class VariablesCompletionSourceTests
{
    private readonly ExecutionThreadBootstrapper _executionThreadBootstrapper = new ExecutionThreadBootstrapper()
        .WithCompletionSource(new VariablesCompletionSource());


    [Theory]
    [InlineData("", "userName")]
    [InlineData("use", "userName")]
    [InlineData("name", "name")]
    [InlineData("-- name", "-")]
    [InlineData("'me' + 3; 'add' || name", "name")]
    [InlineData("callme() + 'asd asd' ;  \n\t \"na\".no[0]", "-")]
    public async Task GetCompletions_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.TopScope.Variables["userName"] = VariantValue.Null;
        thread.TopScope.Variables["name"] = VariantValue.Null;

        // Act.
        var firstCompletion = thread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);

        // Assert.
        Assert.Equal(expected, firstCompletion.Completion.Label);
    }

    [Theory]
    [InlineData("", "userName")]
    [InlineData("name", "name")]
    [InlineData("SELECT 123; SELECT 32 || na", "SELECT 123; SELECT 32 || name")]
    public async Task ApplyCompletion_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        await using var thread = await _executionThreadBootstrapper.CreateAsync();
        thread.TopScope.Variables["userName"] = VariantValue.Null;
        thread.TopScope.Variables["name"] = VariantValue.Null;

        // Act.
        var firstCompletion = thread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);
        var replacedText = firstCompletion.Apply(query);

        // Assert.
        Assert.Equal(expected, replacedText);
    }
}
