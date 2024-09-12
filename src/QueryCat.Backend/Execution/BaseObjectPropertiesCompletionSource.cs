using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

public abstract class BaseObjectPropertiesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public virtual IEnumerable<CompletionResult> Get(CompletionContext context)
    {
        // Example for "Project.Diagrams[0].Na".
        // ObjectExpression = Project.Diagrams[0]", TermSearch = "Na".
        var obj = GetSourceObject(context);
        if (obj == null)
        {
            return [];
        }

        // Pre-calculation for text edit.
        int periodPosition;
        var periodTokenIndex = context.TriggerTokens.FindLastIndex(ParserToken.TokenKindPeriod);
        if (periodTokenIndex > -1)
        {
            periodPosition = context.TriggerTokens[periodTokenIndex].EndIndex;
        }
        else
        {
            periodPosition = context.TriggerTokens.Count > 0 ? context.TriggerTokens[0].StartIndex : 0;
        }

        // Find completions.
        var (_, termSearch) = GetObjectExpressionAndTerm(context.TriggerTokens);
        var completions = GetCompletionItemsByType(obj.GetType(), termSearch);
        return completions.Select(c => new CompletionResult(
            c, [new CompletionTextEdit(periodPosition, periodPosition + termSearch.Length, c.Label)]));
    }

    /// <summary>
    /// Whether the property matches for completion.
    /// </summary>
    /// <param name="propertyInfo">Property.</param>
    /// <returns>Returns <c>true</c> if matches, <c>false</c> otherwise.</returns>
    protected virtual bool IsPropertyMatch(PropertyInfo propertyInfo) => !propertyInfo.Name.StartsWith('.');

    /// <summary>
    /// Get property info documentation text.
    /// </summary>
    /// <param name="propertyInfo">Property.</param>
    /// <returns>Documentation text or empty.</returns>
    protected virtual string GetPropertyDescription(PropertyInfo propertyInfo)
    {
        var description = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        return description != null ? description.Description : string.Empty;
    }

    /// <summary>
    /// Get the source object for the completion.
    /// </summary>
    /// <param name="context">Completion context.</param>
    /// <returns>The source object or null of not found.</returns>
    protected abstract object? GetSourceObject(CompletionContext context);

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
            var relevance = Completion.GetRelevanceByTerm(term, prop.Name);
            if (relevance > 0.0f)
            {
                yield return new Completion(prop.Name, CompletionItemKind.Property, GetPropertyDescription(prop),
                    relevance: relevance);
            }
        }
    }

    internal static (string ObjectExpression, string Term) GetObjectExpressionAndTerm(ParserTokensList triggerTokens)
    {
        var termTokens = triggerTokens.GetRange(triggerTokens.LastSeparatorTokenIndex + 1);

        var periodTokenIndex = termTokens.FindLastIndex(ParserToken.TokenKindPeriod);
        if (periodTokenIndex < 1)
        {
            return (string.Empty, triggerTokens.Any() ? triggerTokens[0].Text : string.Empty);
        }

        return (
            termTokens.Join(0, periodTokenIndex).Trim(),
            termTokens.Join(periodTokenIndex + 1).Trim());
    }
}
