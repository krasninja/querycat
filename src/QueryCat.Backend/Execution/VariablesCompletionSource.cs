using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

public class VariablesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public IEnumerable<CompletionItem> Get(CompletionContext context)
    {
        var lastTokens = context.LastTokens.ToList();
        var spaceTokenIndex = context.LastTokens.ToList()
            .FindLastIndex(t => t.Type != ParserToken.TokenKindPeriod && t.IsSeparator());
        var currentInput = lastTokens[(spaceTokenIndex + 1)..];

        var items = new List<CompletionItem>();
        items.AddRange(FillWithScopesVariables(context, currentInput));

        return items;
    }

    private IEnumerable<CompletionItem> FillWithScopesVariables(CompletionContext context, IReadOnlyList<ParserToken> input)
    {
        // If we have a period - this is already an object, not a variable.
        if (input.Any(i => i.Type == ParserToken.TokenKindPeriod))
        {
            yield break;
        }

        // Try get from variables.
        var searchTerm = context.LastTokens.Any() ? context.LastTokens[^1].Text : string.Empty;
        var scope = context.ExecutionThread.TopScope;
        while (scope != null)
        {
            foreach (var variableName in scope.Variables.Keys)
            {
                var index = variableName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
                if (index == 0)
                {
                    yield return new CompletionItem(variableName, CompletionItemKind.Variable, relevance: 0.7f);
                }
                else if (index > 0)
                {
                    yield return new CompletionItem(variableName, CompletionItemKind.Variable, relevance: 0.2f);
                }
            }
            scope = scope.Parent;
        }
    }
}
