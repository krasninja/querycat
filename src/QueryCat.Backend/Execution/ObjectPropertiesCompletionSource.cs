using System.Diagnostics.CodeAnalysis;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The completion source that uses reflection to get properties from object types.
/// </summary>
public class ObjectPropertiesCompletionSource : ICompletionSource
{
    private static readonly string[] _objectTokens =
    [
        ParserToken.TokenKindPeriod,
        ParserToken.TokenKindLeftBracket,
        ParserToken.TokenKindRightBracket,
    ];

    /// <inheritdoc />
    public IEnumerable<CompletionItem> Get(CompletionContext context)
    {
        // The base pattern is "id.". It means, at least we should have 2 tokens.
        if (context.LastTokens.Count < 2)
        {
            return [];
        }

        var separatorTokenIndex = context.LastTokens.FindLastIndex(
            t => !_objectTokens.Contains(t.Type) && t.IsSeparator());
        var termTokens = context.LastTokens.GetRange(separatorTokenIndex + 1);

        // Example for "Project.Diagrams[0].Na".
        // ObjectExpression = Project.Diagrams[0]", TermSearch = "Na".
        var (objectSelectExpression, termSearch) = GetObjectExpressionAndTerm(termTokens);
        var obj = GetObjectByExpression(context.ExecutionThread, objectSelectExpression);
        if (obj == null)
        {
            return [];
        }

        var periodPosition = context.LastTokens.GetQueryPosition(
            context.LastTokens.FindLastIndex(ParserToken.TokenKindPeriod)) + 1;
        return GetCompletionItemsByType(obj.GetType(), termSearch, periodPosition);
    }

    private static IEnumerable<CompletionItem> GetCompletionItemsByType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        string term,
        int replaceStartIndex)
    {
        var properties = type
            .GetProperties()
            .Where(p => p.CanRead);
        foreach (var prop in properties)
        {
            yield return VariablesCompletionSource.GetCompletionItemByPartialTerm(term, prop.Name, replaceStartIndex);
        }
    }

    private object? GetObjectByExpression(IExecutionThread thread, string objectExpression)
    {
        if (string.IsNullOrEmpty(objectExpression))
        {
            return null;
        }
        try
        {
            var value = thread.Run(objectExpression);
            if (!value.IsNull && value.GetInternalType() == DataType.Object)
            {
                return value.AsObjectUnsafe;
            }
        }
        catch (QueryCatException)
        {
        }

        return VariantValue.Null;
    }

    private static (string ObjectExpression, string Term) GetObjectExpressionAndTerm(ParserTokensList termTokens)
    {
        var periodTokenIndex = termTokens.FindLastIndex(ParserToken.TokenKindPeriod);
        if (periodTokenIndex < 1)
        {
            return (string.Empty, string.Empty);
        }

        return (
            termTokens.Join(0, periodTokenIndex).Trim(),
            termTokens.Join(periodTokenIndex + 1).Trim());
    }
}
