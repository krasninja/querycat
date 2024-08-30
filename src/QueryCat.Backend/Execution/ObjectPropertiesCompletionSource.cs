using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The completion source that uses reflection to get properties from object types.
/// </summary>
public class ObjectPropertiesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public IEnumerable<CompletionResult> Get(CompletionContext context)
    {
        // The base pattern is "id.". It means, at least we should have 2 tokens.
        if (context.TriggerTokens.Count < 2)
        {
            return [];
        }

        var separatorTokenIndex = context.TriggerTokens.FindLastIndex(t => t.IsSeparator());
        if (separatorTokenIndex == context.TriggerTokens.Count - 1)
        {
            return [];
        }

        // Example for "Project.Diagrams[0].Na".
        // ObjectExpression = Project.Diagrams[0]", TermSearch = "Na".
        var termTokens = context.TriggerTokens.GetRange(separatorTokenIndex + 1);
        var (objectSelectExpression, termSearch) = GetObjectExpressionAndTerm(termTokens);
        var obj = GetObjectByExpression(context.ExecutionThread, objectSelectExpression);
        if (obj == null)
        {
            return [];
        }

        var periodTokenIndex = context.TriggerTokens.FindLastIndex(ParserToken.TokenKindPeriod);
        var periodPosition = periodTokenIndex > -1 ? context.TriggerTokens[periodTokenIndex].EndIndex : 0;
        var completions = GetCompletionItemsByType(obj.GetType(), termSearch);
        return completions.Select(c => new CompletionResult(
            c,
            [new CompletionTextEdit(periodPosition, periodPosition + termSearch.Length, c.Label)]));
    }

    /// <summary>
    /// Whether the property matches for completion.
    /// </summary>
    /// <param name="propertyInfo">Property.</param>
    /// <returns>Returns <c>true</c> if matches, <c>false</c> otherwise.</returns>
    public virtual bool IsPropertyMatch(PropertyInfo propertyInfo) => true;

    /// <summary>
    /// Get property info documentation text.
    /// </summary>
    /// <param name="propertyInfo">Property.</param>
    /// <returns>Documentation text or empty.</returns>
    public virtual string GetPropertyDescription(PropertyInfo propertyInfo)
    {
        var description = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        return description != null ? description.Description : string.Empty;
    }

    private IEnumerable<Completion> GetCompletionItemsByType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        string term)
    {
        var properties = type
            .GetProperties()
            .Where(p => p.CanRead)
            .Where(IsPropertyMatch);
        foreach (var prop in properties)
        {
            var completion = VariablesCompletionSource.GetCompletionItemByPartialTerm(
                term, prop.Name, GetPropertyDescription(prop));
            if (completion != null)
            {
                yield return completion;
            }
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

        return null;
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
