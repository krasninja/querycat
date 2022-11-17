using Antlr4.Runtime;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Parser;

/// <summary>
/// The class is to build AST (abstract syntax tree) from user query string.
/// </summary>
internal static class AstBuilder
{
    private static readonly ProgramAntlrErrorListener ErrorListener = new();

    public static ProgramNode BuildProgramFromString(string program)
    {
        var inputStream = new AntlrInputStream(program);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new QueryCatParser(commonTokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(ErrorListener);
        var context = parser.program();
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException("Syntax error.");
        }
        return (ProgramNode)visitor.Visit(context);
    }

    public static FunctionSignatureNode BuildFunctionSignatureFromString(string function)
    {
        var inputStream = new AntlrInputStream(function);
        var lexer = new QueryCatLexer(inputStream);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new QueryCatParser(commonTokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(ErrorListener);
        var context = parser.functionSignature();
        var visitor = new ProgramParserVisitor();
        if (parser.NumberOfSyntaxErrors > 0)
        {
            throw new SyntaxException("Syntax error.");
        }
        return (FunctionSignatureNode)visitor.Visit(context);
    }
}
