using Antlr4.Runtime;
using QueryCat.Backend.Logging;

namespace QueryCat.Backend.Parser;

internal sealed class ProgramAntlrErrorListener : IAntlrErrorListener<IToken>
{
    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        Logger.Instance.Error($"{line}:{charPositionInLine}: {msg}");
    }
}
