using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;

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
    /// <param name="maxItems">Max completion items to get per request. Use -1 to show all.</param>
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
    public async IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_completionSources.Length == 0)
        {
            yield break;
        }
        var items = new List<CompletionResult>(capacity: _maxItems > -1 ? _maxItems : 120);

        var completionIndex = _completionSources.Length;
        foreach (var completionSource in _completionSources)
        {
            var perItem = _maxItems > -1
                ? (_maxItems - items.Count) / completionIndex--
                : int.MaxValue;
            var completions = await completionSource
                .GetAsync(context, cancellationToken)
                .ToListAsync(cancellationToken: cancellationToken);
            var filteredCompletions = completions
                .Where(c => !_preventDuplicates || !HasDuplicates(c, items))
                .Where(IsNotEmpty)
                .OrderByDescending(c => c.Completion.Relevance)
                .Take(perItem);

            items.AddRange(filteredCompletions);
        }

        foreach (var item in items.OrderByDescending(i => i.Completion.Relevance))
        {
            yield return item;
        }
    }

    private bool IsNotEmpty(CompletionResult completionResult) => !string.IsNullOrEmpty(completionResult.Completion.Label);

    private bool HasDuplicates(CompletionResult completionResult, IEnumerable<CompletionResult> currentItems)
    {
        return currentItems.Any(
            i => completionResult.Completion.Kind == i.Completion.Kind
                 && completionResult.Completion.Label.Equals(i.Completion.Label));
    }
}
