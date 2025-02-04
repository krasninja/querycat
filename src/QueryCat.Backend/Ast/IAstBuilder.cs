using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Build abstract syntax tree from string.
/// </summary>
internal interface IAstBuilder
{
    internal readonly struct Token(string text, string type, int startIndex)
    {
        public string Text { get; } = text;

        public string Type { get; } = type;

        public int StartIndex { get; } = startIndex;
    }

    ProgramNode BuildProgramFromString(string program);

    FunctionSignatureNode BuildFunctionSignatureFromString(string function);

    Token[] GetTokens(string text);
}
