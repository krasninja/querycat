using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion source that returns nothing.
/// </summary>
public sealed class NullCompletionSource : ICompletionSource
{
    /// <summary>
    /// Static instance of <see cref="NullCompletionSource" />.
    /// </summary>
    public static NullCompletionSource Instance { get; } = new();

    /// <inheritdoc />
    public IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        return AsyncUtils.Empty<CompletionResult>();
    }
}
