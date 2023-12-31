using Antlr4.Runtime;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Parser;

internal sealed class AstBuilder : IAstBuilder
{
    /// <inheritdoc />
    public ProgramNode BuildProgramFromString(string program) => Build<ProgramNode>(program, p => p.program());

    /// <inheritdoc />
    public FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => Build<FunctionSignatureNode>(function, p => p.functionSignature());

    private static TNode Build<TNode>(string input, Func<QueryCatParser, ParserRuleContext> signatureFunc) where TNode : IAstNode
    {
        var errorListener = new ProgramAntlrErrorListener();

        var inputStream = new AntlrInputStream(input);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new QueryCatParser(commonTokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);
        var context = signatureFunc.Invoke(parser);
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException(errorListener.Message, input, errorListener.Line, errorListener.CharPosition);
        }
        return (TNode)visitor.Visit(context);
    }
}
