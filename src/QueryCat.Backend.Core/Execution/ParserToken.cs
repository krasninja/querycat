using System.Collections.Frozen;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Parser token.
/// </summary>
/// <param name="Text">Token text.</param>
/// <param name="Type">Token type.</param>
/// <param name="StartIndex">Token start index in the text.</param>
public readonly record struct ParserToken(string Text, string Type, int StartIndex)
{
    public const string TokenKindIdentifier = "IDENTIFIER";
    public const string TokenKindSpaces = "SPACES";

    public const string TokenKindLeftParen = "LEFT_PAREN";
    public const string TokenKindRightParen = "RIGHT_PAREN";
    public const string TokenKindAssign = "ASSIGN";
    public const string TokenKindAssociation = "ASSOCIATION";
    public const string TokenKindColon = "COLON";
    public const string TokenKindComma = "COMMA";
    public const string TokenKindPeriod = "PERIOD";
    public const string TokenKindEllipsis = "ELLIPSIS";
    public const string TokenKindSemicolon = "SEMICOLON";
    public const string TokenKindQuestion = "QUESTION";
    public const string TokenKindLeftBracket = "LEFT_BRACKET";
    public const string TokenKindRightBracket = "RIGHT_BRACKET";
    public const string TokenKindPipe = "PIPE";
    public const string TokenKindAtSign = "AT_SIGN";
    public const string TokenKindExclamationSign = "EXCLAMATION_SIGN";
    public const string TokenKindDollarSign = "DOLLAR_SIGN";

    public const string TokenKindPlus = "PLUS";
    public const string TokenKindMinus = "MINUS";
    public const string TokenKindStar = "STAR";
    public const string TokenKindDiv = "DIV";
    public const string TokenKindMod = "MOD";
    public const string TokenKindEquals = "EQUALS";
    public const string TokenKindNotEquals = "NOT_EQUALS";
    public const string TokenKindGreater = "GREATER";
    public const string TokenKindGreaterOrEquals = "GREATER_OR_EQUALS";
    public const string TokenKindLess = "LESS";
    public const string TokenKindLessOrEquals = "LESS_OR_EQUALS";
    public const string TokenKindConcat = "CONCAT";
    public const string TokenKindLessLess = "LESS_LESS";
    public const string TokenKindGreaterGreater = "GREATER_GREATER";

    /// <summary>
    /// End index of the token.
    /// </summary>
    public int EndIndex => StartIndex + Text.Length;

    private static readonly ISet<string> _separatorTokens = new HashSet<string>
    {
        TokenKindSpaces,
        TokenKindSemicolon,

        TokenKindLeftParen,
        TokenKindRightParen,
        TokenKindAssign,
        TokenKindAssociation,
        TokenKindColon,
        TokenKindComma,
        TokenKindEllipsis,
        TokenKindQuestion,
        TokenKindPipe,
        TokenKindAtSign,
        TokenKindExclamationSign,
        TokenKindDollarSign,

        TokenKindPlus,
        TokenKindMinus,
        TokenKindStar,
        TokenKindDiv,
        TokenKindMod,
        TokenKindEquals,
        TokenKindNotEquals,
        TokenKindGreater,
        TokenKindGreaterOrEquals,
        TokenKindLess,
        TokenKindLessOrEquals,
        TokenKindConcat,
        TokenKindLessLess,
        TokenKindGreaterGreater,
    }.ToFrozenSet();

    /// <summary>
    /// Is the separator token (space, operator, etc) between variables.
    /// </summary>
    /// <returns><c>True</c> if separator, <c>false</c> otherwise.</returns>
    public bool IsSeparator() => _separatorTokens.Contains(Type);

    /// <inheritdoc />
    public override string ToString() => $@"{Type}: {Text}";
}
