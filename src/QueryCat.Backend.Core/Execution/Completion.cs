using System.ComponentModel.DataAnnotations;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Completion item.
/// </summary>
public sealed class Completion : ICloneable
{
    /// <summary>
    /// Empty completion.
    /// </summary>
    public static Completion Empty { get; } = new("-", relevance: 0.0f);

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
    /// Does the current completion matches the search term.
    /// </summary>
    public bool IsMatch => this != Empty;

    public Completion(
        string label,
        CompletionItemKind kind = CompletionItemKind.Misc,
        string? documentation = null,
        float relevance = 0.5f)
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

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="item">Clone from other completion item.</param>
    public Completion(Completion item) : this(item.Label, item.Kind, item.Documentation, item.Relevance)
    {
    }

    internal static float GetRelevanceByTerm(string term, string name)
    {
        // Variable starts with the search term. "user" -> "user_name".
        var index = name.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index == 0)
        {
            return 0.7f;
        }

        // Variable starts with the search term. "name" -> "user_name".
        var indexWithUnderscore = name.IndexOf("_" + term, StringComparison.OrdinalIgnoreCase);
        if (indexWithUnderscore > -1)
        {
            return 0.6f;
        }

        // Variable name contains the term. "me" -> "user_name".
        if (index > 0)
        {
            return 0.4f;
        }

        // No matches.
        return Empty.Relevance;
    }

    /// <inheritdoc />
    public object Clone() => new Completion(this);

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {Label}";
}
