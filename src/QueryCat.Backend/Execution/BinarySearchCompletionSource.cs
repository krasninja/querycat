using System.Globalization;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

public abstract class BinarySearchCompletionSource : ICompletionSource
{
    private readonly Completion[] _completions;

    protected BinarySearchCompletionSource(IEnumerable<Completion> completions)
    {
        _completions = completions.OrderBy(c => c.Label).ToArray();
    }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        CancellationToken cancellationToken = default);

    protected IEnumerable<Completion> GetCompletionsStartsWith(string term)
        => GetCompletionsStartsWith(_completions, term);

    protected static IEnumerable<Completion> GetCompletionsStartsWith(Completion[] completions, string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            foreach (var completion in completions)
            {
                yield return completion;
            }
        }

        var startIndex = BinarySearchStartsWith(completions, term);
        if (startIndex < 0)
        {
            yield break;
        }

        for (var i = startIndex - 1; i > 0; i--)
        {
            var completion = completions[i];
            if (completion.Label.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return completion;
            }
            else
            {
                break;
            }
        }

        for (var i = startIndex; i < completions.Length; i++)
        {
            var completion = completions[i];
            if (completion.Label.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return completion;
            }
            else
            {
                break;
            }
        }
    }

    private static int BinarySearchStartsWith(Completion[] completions, string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return -1;
        }
        var compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        var min = 0;
        var max = completions.Length - 1;
        while (max >= min)
        {
            var mid = min + ((max - min) >> 1);
            var len = Math.Min(completions[mid].Label.Length, term.Length);
            var label = completions[mid].Label.AsSpan()[..len];
            var index = compareInfo.Compare(label, term, CompareOptions.IgnoreCase);
            if (index == 0)
            {
                return mid;
            }
            if (index < 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid - 1;
            }
        }
        return -1;
    }
}
