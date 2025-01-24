using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitFunctionSignature(QueryCatParser.FunctionSignatureContext context)
        => new FunctionSignatureNode(
            GetUnwrappedText(context.name),
            this.Visit<FunctionTypeNode>(context.functionType()),
            this.Visit<FunctionSignatureArgumentNode>(context.functionArg()));

    /// <inheritdoc />
    public override IAstNode VisitFunctionType(QueryCatParser.FunctionTypeContext context)
    {
        return new FunctionTypeNode(
            this.Visit<TypeNode>(context.type()).Type, GetUnwrappedText(context.identifierSimple()));
    }

    /// <inheritdoc />
    public override IAstNode VisitFunctionArg(QueryCatParser.FunctionArgContext context)
        => new FunctionSignatureArgumentNode(
            name: context.identifierSimple().GetText(),
            typeNode: this.Visit<FunctionTypeNode>(context.functionType()),
            defaultValue: this.Visit(context.@default, LiteralNode.NullValueNode).Value,
            isOptional: context.optional != null,
            isArray: context.isArray != null,
            isVariadic: context.variadic != null);
}
