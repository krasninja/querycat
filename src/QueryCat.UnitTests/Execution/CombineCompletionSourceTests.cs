using QueryCat.Backend;
using QueryCat.Backend.Execution;
using Xunit;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="CombineCompletionSource" />.
/// </summary>
public sealed class CombineCompletionSourceTests
{
    [Fact]
    public void GetCompletions_DuplicatedSource_ShouldPreventDuplicates()
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
        using var executionThread = new ExecutionThreadBootstrapper()
            .WithCompletionSource(combineCompletionSource)
            .Create();

        // Act.
        var completionsCount = executionThread.GetCompletions("SELEC").Count();

        // Assert.
        Assert.Equal(1, completionsCount);
    }
}
