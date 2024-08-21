using System.ComponentModel.DataAnnotations;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion item.
/// </summary>
public readonly record struct CompletionItem
{
    /// <summary>
    /// Empty completion.
    /// </summary>
    public static CompletionItem Empty { get; } = new("-", CompletionItemKind.Misc, relevance: 0.0f);

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
    public double Relevance { get; }

    public CompletionItem(string label, CompletionItemKind kind, string? documentation = null, double relevance = 0.5f)
    {
        if (relevance < 0.0f || relevance > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(relevance));
        }

        Label = label;
        Documentation = documentation ?? string.Empty;
        Kind = kind;
        Relevance = relevance;
    }
}
