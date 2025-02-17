namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Source of autocomplete items.
/// </summary>
public interface ICompletionSource
{
    /// <summary>
    /// Get completion items based on current context.
    /// </summary>
    /// <param name="context">Completion context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completion result.</returns>
    IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        CancellationToken cancellationToken = default);
}
