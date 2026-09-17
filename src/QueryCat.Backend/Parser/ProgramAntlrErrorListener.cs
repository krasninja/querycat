using Antlr4.Runtime;

namespace QueryCat.Backend.Parser;

internal sealed class ProgramAntlrErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
{
    public int Line { get; private set; } = -1;

    public int CharPosition { get; private set; } = -1;

    public string Message { get; private set; } = string.Empty;

    public bool HasError => Line > -1 && CharPosition > -1;

    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        if (HasError)
        {
            return;
        }
        Line = line;
        CharPosition = charPositionInLine;
        Message = msg;
    }

    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        if (HasError)
        {
            return;
        }
        Line = line;
        CharPosition = charPositionInLine;
        Message = msg;
    }

    public void Clear()
    {
        Line = -1;
        CharPosition = -1;
        Message = string.Empty;
    }
}
