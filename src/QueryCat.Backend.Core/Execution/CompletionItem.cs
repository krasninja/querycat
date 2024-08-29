using System.ComponentModel.DataAnnotations;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion item.
/// </summary>
public sealed class CompletionItem : ICloneable
{
    /// <summary>
    /// Empty completion.
    /// </summary>
    public static CompletionItem Empty { get; } = new("-", relevance: 0.0f);

    /// <summary>
    /// The text of the completion (variable name, function signature, etc).
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Kind (type).
    /// </summary>
    public CompletionItemKind Kind { get; }

    /// <summary>
    /// Short optional documentation text.
    /// </summary>
    public string Documentation { get; }

    /// <summary>
    /// Relevance between 0 and 1. The most means better relevance.
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float Relevance { get; }

    /// <summary>
    /// The index in the query where the completion starts.
    /// </summary>
    public int ReplaceStartIndex { get; }

    /// <summary>
    /// Custom user tag.
    /// </summary>
    public object? Tag { get; }

    public CompletionItem(
        string label,
        CompletionItemKind kind = CompletionItemKind.Misc,
        string? documentation = null,
        float relevance = 0.5f,
        int replaceStartIndex = 0,
        object? tag = null)
    {
        if (relevance < 0.0f || relevance > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(relevance));
        }

        Label = label;
        Documentation = documentation ?? string.Empty;
        Kind = kind;
        Relevance = relevance;
        Tag = tag;
        ReplaceStartIndex = replaceStartIndex;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="item">Clone from other completion item.</param>
    public CompletionItem(CompletionItem item)
        : this(item.Label, item.Kind, item.Documentation, item.Relevance, item.ReplaceStartIndex, item.Tag)
    {
    }

    /// <inheritdoc />
    public object Clone() => new CompletionItem(this);

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {Label}";
}
