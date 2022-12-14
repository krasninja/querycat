using Serilog;
using Antlr4.Runtime;

namespace QueryCat.Backend.Parser;

internal sealed class ProgramAntlrErrorListener : IAntlrErrorListener<IToken>
{
    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        Log.Logger.Error("{Line}:{Position}: {Message}", line, charPositionInLine, msg);
    }
}
