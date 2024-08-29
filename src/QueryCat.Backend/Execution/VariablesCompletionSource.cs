using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

public class VariablesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public IEnumerable<CompletionItem> Get(CompletionContext context)
    {
        var separatorTokenIndex = context.LastTokens.FindLastIndex(t => t.IsSeparator());
        var currentInput = context.LastTokens.GetRange(separatorTokenIndex + 1);

        var items = new List<CompletionItem>();
        items.AddRange(FillWithScopesVariables(context, currentInput));

        return items;
    }

    private IEnumerable<CompletionItem> FillWithScopesVariables(CompletionContext context, ParserTokensList input)
    {
        // If we have a period - this is already an object, not a variable.
        if (input.FindIndex(ParserToken.TokenKindPeriod) > -1)
        {
            yield break;
        }

        // Try to get from variables.
        var searchTerm = context.LastTokens.Any() ? context.LastTokens.Last().Text : string.Empty;
        var scope = context.ExecutionThread.TopScope;
        var replaceStartIndex = context.Text.Length - searchTerm.Length;
        while (scope != null)
        {
            foreach (var variableName in scope.Variables.Keys)
            {
                var completion = GetCompletionItemByPartialTerm(searchTerm, variableName, replaceStartIndex);
                if (completion != null)
                {
                    yield return completion;
                }
            }
            scope = scope.Parent;
        }
    }

    internal static CompletionItem? GetCompletionItemByPartialTerm(string term, string variableName, int replaceStartIndex = 0)
    {
        var index = variableName.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index == 0)
        {
            return new CompletionItem(variableName, CompletionItemKind.Variable, relevance: 0.7f,
                replaceStartIndex: replaceStartIndex);
        }
        if (index > 0)
        {
            return new CompletionItem(variableName, CompletionItemKind.Variable, relevance: 0.5f,
                replaceStartIndex: replaceStartIndex);
        }
        return null;
    }
}
