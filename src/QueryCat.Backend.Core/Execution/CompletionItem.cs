using System.ComponentModel.DataAnnotations;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion item.
/// </summary>
public sealed class CompletionItem
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
    /// Custom user tag.
    /// </summary>
    public object? Tag { get; }

    public CompletionItem(
        string label,
        CompletionItemKind kind = CompletionItemKind.Misc,
        string? documentation = null,
        float relevance = 0.5f,
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
    }

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {Label}";
}
