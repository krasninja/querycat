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
    public abstract IEnumerable<CompletionResult> Get(CompletionContext context);

    protected IEnumerable<Completion> GetCompletionsStartsWith(string term)
        => GetCompletionsStartsWith(_completions, term);

    protected static IEnumerable<Completion> GetCompletionsStartsWith(Completion[] completions, string term)
    {
        var startIndex = BinarySearchStartsWith(completions, term);
        if (startIndex < 0)
        {
            foreach (var completion in completions)
            {
                yield return completion;
            }
        }
        else
        {
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
    }

    private static int BinarySearchStartsWith(Completion[] completions, string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return -1;
        }
        var compareInfo = CultureInfo.InvariantCulture.CompareInfo;

        var min = 0;
        var max = completions.Length;
        while (max >= min)
        {
            var mid = (min + max) / 2;
            var len = Math.Min(completions[mid].Label.Length, term.Length);
            var label = completions[mid].Label.AsSpan()[..len];
            var index = compareInfo.Compare(label, term, CompareOptions.IgnoreCase);
            if (index < 0)
            {
                min = mid + 1;
            }
            else if (index > 0)
            {
                max = mid - 1;
            }
            else
            {
                return mid;
            }
        }
        return -1;
    }
}
