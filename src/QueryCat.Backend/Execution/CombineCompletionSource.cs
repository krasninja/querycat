using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Completion source that can combine multiple other completion sources.
/// </summary>
public sealed class CombineCompletionSource : ICompletionSource
{
    private readonly ICompletionSource[] _completionSources;
    private readonly int _maxItems;
    private readonly bool _preventDuplicates;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="completionSources">Completion source.</param>
    /// <param name="maxItems">Max completion items to get per request.</param>
    public CombineCompletionSource(
        IEnumerable<ICompletionSource> completionSources, int maxItems = 10)
        : this(completionSources.ToArray(), maxItems)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="completionSources">Completion source.</param>
    /// <param name="maxItems">Max completion items to get per request.</param>
    /// <param name="preventDuplicates">Prevent duplicate items.</param>
    public CombineCompletionSource(
        ICompletionSource[] completionSources,
        int maxItems = 10,
        bool preventDuplicates = false)
    {
        _completionSources = completionSources;
        _maxItems = maxItems;
        _preventDuplicates = preventDuplicates;
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
                .ToList();
            if (_preventDuplicates)
            {
                completions.RemoveAll(c => items.Any(
                    i => c.Completion.Kind == i.Completion.Kind && c.Completion.Label.Equals(i.Completion.Label)));
            }
            items.AddRange(completions);
            remainItems -= completions.Count;
        }

        return items.OrderByDescending(i => i.Completion.Relevance);
    }
}
