using Antlr4.Runtime;

namespace QueryCat.Backend.Parser;

internal sealed class ProgramAntlrErrorListener : IAntlrErrorListener<IToken>
{
    public int Line { get; private set; } = -1;

    public int CharPosition { get; private set; } = -1;

    public string Message { get; private set; } = string.Empty;

    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        Line = line;
        CharPosition = charPositionInLine;
        Message = msg;
    }
}
