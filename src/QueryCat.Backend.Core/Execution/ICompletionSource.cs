namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Source of autocomplete items.
/// </summary>
public interface ICompletionSource
{
    /// <summary>
    /// Get completion items based on current context.
    /// </summary>
    /// <param name="context">Completion context that has reference to execution thread and position.</param>
    /// <returns>Completion items.</returns>
    IEnumerable<CompletionItem> Get(CompletionContext context);
}
