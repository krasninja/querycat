using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="VariablesCompletionSource" />.
/// </summary>
public sealed class VariablesCompletionSourceTests : IDisposable
{
    private readonly IExecutionThread _executionThread;

    public VariablesCompletionSourceTests()
    {
        _executionThread = new ExecutionThreadBootstrapper()
            .WithCompletionSource(new VariablesCompletionSource())
            .Create();
    }

    [Theory]
    [InlineData("", "userName")]
    [InlineData("use", "userName")]
    [InlineData("name", "name")]
    [InlineData("-- name", "-")]
    [InlineData("'me' + 3; 'add' || name", "name")]
    [InlineData("callme() + 'asd asd' ;  \n\t \"na\".no[0]", "-")]
    public void GetCompletions_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        _executionThread.TopScope.Variables["userName"] = VariantValue.Null;
        _executionThread.TopScope.Variables["name"] = VariantValue.Null;

        // Act.
        var firstCompletion = _executionThread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);

        // Assert.
        Assert.Equal(expected, firstCompletion.Completion.Label);
    }

    [Theory]
    [InlineData("", "userName")]
    [InlineData("name", "name")]
    [InlineData("SELECT 123; SELECT 32 || na", "SELECT 123; SELECT 32 || name")]
    public void ApplyCompletion_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        _executionThread.TopScope.Variables["userName"] = VariantValue.Null;
        _executionThread.TopScope.Variables["name"] = VariantValue.Null;

        // Act.
        var firstCompletion = _executionThread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);
        var replacedText = firstCompletion.Apply(query);

        // Assert.
        Assert.Equal(expected, replacedText);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _executionThread.Dispose();
    }
}
