using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Build abstract syntax tree from string.
/// </summary>
internal interface IAstBuilder
{
    ProgramNode BuildProgramFromString(string program);

    FunctionSignatureNode BuildFunctionSignatureFromString(string function);
}
