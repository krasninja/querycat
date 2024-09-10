using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="KeywordsCompletionSourceTests" />.
/// </summary>
public sealed class KeywordsCompletionSourceTests
{
    [Fact]
    public void GetCompletions_InTerm_CorrectKeywords()
    {
        // Arrange.
        var combineCompletionSource = new CombineCompletionSource([new KeywordsCompletionSource()]);
        using var executionThread = new ExecutionThreadBootstrapper()
            .WithCompletionSource(combineCompletionSource)
            .Create();

        // Act.
        var completions = executionThread.GetCompletions("in")
            .Select(c => c.Completion.Label).OrderBy(c => c).ToArray();

        // Assert.
        Assert.Equal<string[]>(
            ["IN", "INNER", "INSERT", "INT", "INT8", "INTEGER", "INTERSECT", "INTERVAL", "INTO"],
            completions);
    }
}
