using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.FunctionsManager;

public sealed class FunctionsCompletionSource : ICompletionSource
{
    private readonly IFunctionsManager _functionsManager;
    private Completion[] _completions = [];
    private bool _isInitialized;

    public FunctionsCompletionSource(IFunctionsManager functionsManager)
    {
        _functionsManager = functionsManager;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        // If we have a period - this is already an object, not a variable.
        if (context.TriggerTokens.FindIndex(ParserToken.TokenKindPeriod) > -1)
        {
            return AsyncUtils.Empty<CompletionResult>();
        }

        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        IEnumerable<CompletionResult> Filter(IEnumerable<Completion> completions)
        {
            var searchTerm = context.LastTokenText;
            foreach (var completion in completions)
            {
                var relevance = Completion.GetRelevanceByTerm(searchTerm, completion.Label);
                if (relevance > 0.0f)
                {
                    var textEdit = new CompletionTextEdit(
                        context.TriggerTokenPosition,
                        context.TriggerTokenPosition + searchTerm.Length,
                        completion.Label);
                    yield return new CompletionResult(completion, [textEdit]);
                }
            }
        }

        return AsyncUtils.ToAsyncEnumerable(Filter(_completions));
    }

    private void Initialize()
    {
        _completions = _functionsManager
            .GetFunctions()
            .Select(f => new Completion(f.Name, CompletionItemKind.Variable, f.Description, relevance: 0.6f))
            .ToArray();
    }
}
