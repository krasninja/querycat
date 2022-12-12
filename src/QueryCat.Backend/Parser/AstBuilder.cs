using Antlr4.Runtime;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Parser;

/// <summary>
/// The class is to build AST (abstract syntax tree) from user query string.
/// </summary>
internal static class AstBuilder
{
    private static readonly ProgramAntlrErrorListener ErrorListener = new();

    public static ProgramNode BuildProgramFromString(string program)
        => Build<ProgramNode>(program, p => p.program());

    public static FunctionSignatureNode BuildFunctionSignatureFromString(string function)
        => Build<FunctionSignatureNode>(function, p => p.functionSignature());

    private static TNode Build<TNode>(string input, Func<QueryCatParser, ParserRuleContext> signatureFunc) where TNode : IAstNode
    {
        var inputStream = new AntlrInputStream(input);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new QueryCatParser(commonTokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(ErrorListener);
        var context = signatureFunc.Invoke(parser);
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException("Syntax error.");
        }
        return (TNode)visitor.Visit(context);
    }
}
