using QueryCat.Backend;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
using Xunit;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="CombineCompletionSource" />.
/// </summary>
public sealed class CombineCompletionSourceTests
{
    [Fact]
    public async Task GetCompletions_DuplicatedSource_ShouldPreventDuplicates()
    {
        // Arrange.
        var combineCompletionSource = new CombineCompletionSource(
            [
                new KeywordsCompletionSource(),
                new KeywordsCompletionSource(),
                new KeywordsCompletionSource()
            ],
            maxItems: -1,
            preventDuplicates: true);
        await using var executionThread = await new ExecutionThreadBootstrapper()
            .WithCompletionSource(combineCompletionSource)
            .CreateAsync();

        // Act.
        var completionsCount = (await executionThread.GetCompletionsAsync("SELEC").ToListAsync()).Count;

        // Assert.
        Assert.Equal(1, completionsCount);
    }
}
