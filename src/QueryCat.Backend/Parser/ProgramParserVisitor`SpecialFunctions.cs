using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitSimpleExpressionBinaryCast(QueryCatParser.SimpleExpressionBinaryCastContext context)
        => new CastFunctionNode(
            this.Visit<ExpressionNode>(context.right),
            this.VisitType(context.type())
        );

    /// <inheritdoc />
    public override IAstNode VisitStandardFunctionCoalesce(QueryCatParser.StandardFunctionCoalesceContext context)
        => new CoalesceFunctionNode(this.Visit<ExpressionNode>(context.expression()).ToList());
}
