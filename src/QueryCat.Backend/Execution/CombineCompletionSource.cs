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
    public IEnumerable<CompletionResult> Get(CompletionContext context)
    {
        if (_completionSources.Length == 0)
        {
            return [];
        }
        var remainItems = _maxItems;
        var items = new List<CompletionResult>(capacity: _maxItems);

        var completionIndex = _completionSources.Length;
        foreach (var completionSource in _completionSources)
        {
            var perItem = remainItems / completionIndex--;
            var completions = completionSource
                .Get(context)
                .OrderByDescending(c => c.Completion.Relevance)
                .Take(perItem)
                .ToArray();
            items.AddRange(completions);
            remainItems -= completions.Length;
        }

        return items.OrderByDescending(i => i.Completion.Relevance);
    }
}
