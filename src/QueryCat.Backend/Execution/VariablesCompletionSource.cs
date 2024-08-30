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
        var searchTerm = context.TriggerTokens.Any() ? context.TriggerTokens.Last().Text : string.Empty;
        var scope = context.ExecutionThread.TopScope;
        while (scope != null)
        {
            foreach (var variableName in scope.Variables.Keys)
            {
                var completion = GetCompletionItemByPartialTerm(searchTerm, variableName, string.Empty);
                if (completion != null)
                {
                    var textEdit = new CompletionTextEdit(
                        context.TriggerTokenPosition,
                        context.TriggerTokenPosition + searchTerm.Length,
                        variableName);
                    yield return new CompletionResult(completion, [textEdit]);
                }
            }
            scope = scope.Parent;
        }
    }

    internal static Completion? GetCompletionItemByPartialTerm(string term, string variableName, string documentation)
    {
        var index = variableName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index == 0)
        {
            return new Completion(variableName, CompletionItemKind.Variable, documentation, relevance: 0.7f);
        }
        if (index > 0)
        {
            return new Completion(variableName, CompletionItemKind.Variable, documentation, relevance: 0.5f);
        }
        return null;
    }
}
