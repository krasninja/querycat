using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Completion source that can combine multiple other completion sources.
/// </summary>
internal sealed class CombineCompletionSource : ICompletionSource
{
    private readonly ICompletionSource[] _completionSources;
    private readonly int _maxItems;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="completionSources">Completion source.</param>
    /// <param name="maxItems">Max completion items to get per request.</param>
    public CombineCompletionSource(IEnumerable<ICompletionSource> completionSources, int maxItems = 10)
        : this(completionSources.ToArray(), maxItems)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="completionSources">Completion source.</param>
    /// <param name="maxItems">Max completion items to get per request.</param>
    public CombineCompletionSource(ICompletionSource[] completionSources, int maxItems = 10)
    {
        _completionSources = completionSources;
        _maxItems = maxItems;
    }

    /// <inheritdoc />
    public IEnumerable<CompletionItem> Get(CompletionContext context)
    {
        if (_completionSources.Length == 0)
        {
            return [];
        }
        var perItem = _maxItems / _completionSources.Length;
        var items = new List<CompletionItem>(capacity: _maxItems);

        foreach (var completionSource in _completionSources)
        {
            items.AddRange(completionSource.Get(context).Take(perItem));
        }

        return items.OrderByDescending(i => i.Relevance);
    }
}
