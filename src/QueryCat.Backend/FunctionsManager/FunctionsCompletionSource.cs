using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;

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
    public async IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // If we have a period - this is already an object, not a variable.
        if (context.TriggerTokens.FindIndex(ParserToken.TokenKindPeriod) > -1)
        {
            yield break;
        }

        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        var searchTerm = context.LastTokenText;
        foreach (var completion in _completions)
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

    private void Initialize()
    {
        _completions = _functionsManager
            .GetFunctions()
            .Select(f => new Completion(f.Name, CompletionItemKind.Variable, f.Description, relevance: 0.6f))
            .ToArray();
    }
}
