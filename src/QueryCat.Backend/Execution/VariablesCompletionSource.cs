using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Completion source that uses thread variables for autocomplete.
/// </summary>
public class VariablesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public async IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var items = new List<CompletionResult>();
        items.AddRange(FillWithScopesVariables(context));

        foreach (var item in items)
        {
            yield return item;
        }
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
            foreach (var variable in scope.Variables)
            {
                if (!IsVariableMatch(variable.Key, variable.Value))
                {
                    continue;
                }
                var relevance = Completion.GetRelevanceByTerm(searchTerm, variable.Key);
                if (relevance == 0.0f)
                {
                    continue;
                }

                var textEdit = new CompletionTextEdit(
                    context.TriggerTokenPosition,
                    context.TriggerTokenPosition + searchTerm.Length,
                    variable.Key);
                yield return new CompletionResult(
                    new Completion(variable.Key, CompletionItemKind.Variable, relevance: relevance), [textEdit]);
            }
            scope = scope.Parent;
        }
    }

    /// <summary>
    /// Helps to filter variables.
    /// </summary>
    /// <param name="name">Variable name.</param>
    /// <param name="value">Variable value.</param>
    /// <returns>Returns <c>true</c> if matches, <c>false</c> otherwise.</returns>
    protected virtual bool IsVariableMatch(string name, in VariantValue value)
    {
        return !name.StartsWith('.');
    }
}
