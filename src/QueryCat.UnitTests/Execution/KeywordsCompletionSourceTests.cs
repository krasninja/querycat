using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="KeywordsCompletionSourceTests" />.
/// </summary>
public sealed class KeywordsCompletionSourceTests
{
    [Fact]
    public async Task GetCompletions_InTerm_CorrectKeywords()
    {
        // Arrange.
        var combineCompletionSource = new CombineCompletionSource([new KeywordsCompletionSource()]);
        await using var executionThread = await new ExecutionThreadBootstrapper()
            .WithCompletionSource(combineCompletionSource)
            .CreateAsync();

        // Act.
        var completions = (await executionThread.GetCompletionsAsync("in").ToListAsync())
            .Select(c => c.Completion.Label).OrderBy(c => c).ToArray();

        // Assert.
        Assert.Equal<string[]>(
            ["IN", "INNER", "INSERT", "INT", "INT8", "INTEGER", "INTERSECT", "INTERVAL", "INTO"],
            completions);
    }
}
