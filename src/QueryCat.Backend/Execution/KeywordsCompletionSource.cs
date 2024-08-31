using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The completion that helps to fill most of the keywords.
/// </summary>
public sealed class KeywordsCompletionSource : ICompletionSource
{
    private static readonly string[] _keywords =
    [
        "ANY",
        "BLOB",
        "BOOL",
        "BOOLEAN",
        "DECIMAL",
        "FLOAT",
        "INT",
        "INT8",
        "INTEGER",
        "NUMERIC",
        "OBJECT",
        "REAL",
        "STRING",
        "TEXT",
        "TIMESTAMP",

        "AND",
        "AS",
        "AT",
        "BEGIN",
        "BY",
        "CAST",
        "DEFAULT",
        "END",
        "EXISTS",
        "FALSE",
        "FROM",
        "IN",
        "IS",
        "LIKE",
        "LIKE_REGEX",
        "NOT",
        "NULL",
        "ON",
        "ONLY",
        "OR",
        "SOME",
        "TO",
        "TRUE",
        "USING",
        "VOID",

        "TRIM",
        "LEADING",
        "TRAILING",
        "BOTH",

        "CURRENT_DATE",
        "CURRENT_TIMESTAMP",
        "INTERVAL",
        "YEAR",
        "DOY",
        "DAYOFYEAR",
        "MONTH",
        "DOW",
        "WEEKDAY",
        "DAY",
        "HOUR",
        "MINUTE",
        "SECOND",
        "MILLISECOND",
        "LOCAL",
        "TIME",
        "ZONE",

        "CASE",
        "COALESCE",
        "EXTRACT",
        "POSITION",
        "WHEN",
        "OCCURRENCES_REGEX",
        "SUBSTRING_REGEX",
        "POSITION_REGEX",
        "TRANSLATE_REGEX",

        "ECHO",

        "ALL",
        "ASC",
        "BETWEEN",
        "CURRENT",
        "DESC",
        "DISTINCT",
        "EXCEPT",
        "FETCH",
        "FIRST",
        "FOLLOWING",
        "FORMAT",
        "FULL",
        "GROUP",
        "HAVING",
        "INNER",
        "INTERSECT",
        "INTO",
        "JOIN",
        "LAST",
        "LEFT",
        "LIMIT",
        "NEXT",
        "NULLS",
        "OFFSET",
        "ORDER",
        "OUTER",
        "OVER",
        "PARTITION",
        "PRECEDING",
        "RECURSIVE",
        "RIGHT",
        "ROW",
        "ROWS",
        "SELECT",
        "SIMILAR",
        "TOP",
        "UNBOUNDED",
        "UNION",
        "VALUES",
        "WHERE",
        "WINDOW",
        "WITH",

        "UPDATE",

        "INSERT",

        "DECLARE",
        "SET",

        "CALL",

        "IF",
        "THEN",
        "ELSE",
        "ELSEIF",

        "WHILE",
        "BREAK",
        "CONTINUE",
        "FOR",
    ];

    private readonly IEnumerable<Completion> _keywordsCompletions;

    public KeywordsCompletionSource()
    {
        _keywordsCompletions = _keywords
            .Select(k => new Completion(k, CompletionItemKind.Keyword, relevance: 0.6f))
            .ToList();
    }

    /// <inheritdoc />
    public IEnumerable<CompletionResult> Get(CompletionContext context)
    {
        if (context.TriggerTokens.FindIndex(ParserToken.TokenKindPeriod) > -1
            || context.TriggerTokens.FindIndex(ParserToken.TokenKindLeftBracket) > -1
            || context.TriggerTokens.FindIndex(ParserToken.TokenKindRightBracket) > -1)
        {
            return [];
        }

        var searchTerm = context.TriggerTokens.Any() ? context.TriggerTokens[^1].Text : string.Empty;

        return _keywordsCompletions
            .Where(c => c.Label.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Select(c => new CompletionResult(c, [
                new CompletionTextEdit(context.TriggerTokenPosition, context.TriggerTokenPosition + searchTerm.Length, c.Label)
            ]));
    }
}
