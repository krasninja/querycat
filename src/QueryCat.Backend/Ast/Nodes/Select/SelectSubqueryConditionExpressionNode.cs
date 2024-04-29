using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectSubqueryConditionExpressionNode : ExpressionNode
{
    public enum QuantifierOperator
    {
        Any,
        All
    }

    /// <inheritdoc />
    public override string Code => "subquery_op";

    public ExpressionNode LeftNode { get; }

    public VariantValue.Operation Operation { get; }

    public QuantifierOperator Operator { get; }

    public SelectQueryNode SubQueryNode { get; }

    /// <inheritdoc />
    public SelectSubqueryConditionExpressionNode(
        ExpressionNode leftNode,
        VariantValue.Operation operation,
        QuantifierOperator quantifierOperator,
        SelectQueryNode subQueryNode)
    {
        LeftNode = leftNode;
        Operation = operation;
        Operator = quantifierOperator;
        SubQueryNode = subQueryNode;
    }

    public SelectSubqueryConditionExpressionNode(SelectSubqueryConditionExpressionNode node)
        : this(
            (ExpressionNode)node.LeftNode.Clone(),
            node.Operation,
            node.Operator,
            (SelectQueryNode)node.SubQueryNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectSubqueryConditionExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return LeftNode;
        yield return SubQueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
