using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.If;

namespace QueryCat.Backend.Parser;

internal partial class ProgramParserVisitor
{
    /// <inheritdoc />
    public override IAstNode VisitIfStatement(QueryCatParser.IfStatementContext context)
        => new IfConditionStatementNode(
            new IfConditionNode(
                this.Visit<IfConditionItemNode>(context.ifCondition()).ToList(),
                elseNode: context.elseBlock != null ? this.Visit<BlockExpressionNode>(context.elseBlock) : null));

    /// <inheritdoc />
    public override IAstNode VisitIfCondition(QueryCatParser.IfConditionContext context)
        => new IfConditionItemNode(
            this.Visit<ExpressionNode>(context.condition),
            this.Visit<BlockExpressionNode>(context.block));
}
