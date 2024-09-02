using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Completion source that uses thread variables for autocomplete.
/// </summary>
public class VariablesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public IEnumerable<CompletionResult> Get(CompletionContext context)
    {
        var items = new List<CompletionResult>();
        items.AddRange(FillWithScopesVariables(context));

        return items;
    }

    private IEnumerable<CompletionResult> FillWithScopesVariables(CompletionContext context)
    {
        // If we have a period - this is already an object, not a variable.
        if (context.TriggerTokens.FindIndex(ParserToken.TokenKindPeriod) > -1)
        {
            yield break;
        }

        // Try to get from variables.
        var searchTerm = context.LastTokenText;
        var scope = context.ExecutionThread.TopScope;
        while (scope != null)
        {
            foreach (var variableName in scope.Variables.Keys)
            {
                var relevance = Completion.GetRelevanceByTerm(searchTerm, variableName);
                if (relevance > 0.0f)
                {
                    var textEdit = new CompletionTextEdit(
                        context.TriggerTokenPosition,
                        context.TriggerTokenPosition + searchTerm.Length,
                        variableName);
                    yield return new CompletionResult(
                        new Completion(variableName, CompletionItemKind.Variable, relevance: relevance), [textEdit]);
                }
            }
            scope = scope.Parent;
        }
    }
}
