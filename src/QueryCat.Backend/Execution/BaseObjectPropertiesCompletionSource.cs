using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

public abstract class BaseObjectPropertiesCompletionSource : ICompletionSource
{
    /// <inheritdoc />
    public virtual async IAsyncEnumerable<CompletionResult> GetAsync(CompletionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Example for "Project.Diagrams[0].Na".
        // ObjectExpression = Project.Diagrams[0]", TermSearch = "Na".
        var obj = await GetSourceObjectAsync(context, cancellationToken);
        if (obj == null)
        {
            yield break;
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
        var items = completions.Select(c => new CompletionResult(
            c, [new CompletionTextEdit(periodPosition, periodPosition + termSearch.Length, c.Label)]));
        foreach (var item in items)
        {
            yield return item;
        }
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The source object or null of not found.</returns>
    protected abstract ValueTask<object?> GetSourceObjectAsync(CompletionContext context, CancellationToken cancellationToken);

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

    /// <summary>
    /// Return object expression and search term. For example, "User.Addresses[0].Ci": (User.Addresses[0], Ci).
    /// </summary>
    /// <param name="triggerTokens">Tokens.</param>
    /// <returns>Object expression and search term.</returns>
    internal static (string ObjectExpression, string Term) GetObjectExpressionAndTerm(ParserTokensList triggerTokens)
    {
        var termTokens = triggerTokens.GetRange(triggerTokens.LastSeparatorTokenIndex + 1);

        var periodTokenIndex = termTokens.FindLastIndex(ParserToken.TokenKindPeriod);
        if (periodTokenIndex < 1)
        {
            return (
                ObjectExpression: string.Empty,
                Term: triggerTokens.Any() ? triggerTokens[0].Text.Trim() : string.Empty
            );
        }

        return (
            ObjectExpression: termTokens.Join(0, periodTokenIndex).Trim(),
            Term: termTokens.Join(periodTokenIndex + 1).Trim()
        );
    }
}
