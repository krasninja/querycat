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
    /// <returns>Completion result.</returns>
    IEnumerable<CompletionResult> Get(CompletionContext context);
}
