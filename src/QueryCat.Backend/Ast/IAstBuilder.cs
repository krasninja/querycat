using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Build abstract syntax tree from string.
/// </summary>
internal interface IAstBuilder
{
    internal readonly struct Token(string text, string type)
    {
        public string Text { get; } = text;

        public string Type { get; } = type;
    }

    ProgramNode BuildProgramFromString(string program);

    FunctionSignatureNode BuildFunctionSignatureFromString(string function);

    IReadOnlyList<Token> GetTokens(string text);
}
