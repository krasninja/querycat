using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectSubqueryConditionExpressionNode : ExpressionNode
{
    public enum QuantifierOperator
    {
        Any,
        All
    }

    public ExpressionNode Left { get; }

    public VariantValue.Operation Operation { get; }

    public QuantifierOperator Operator { get; }

    public SelectQueryExpressionBodyNode SubQueryNode { get; }

    /// <inheritdoc />
    public SelectSubqueryConditionExpressionNode(
        ExpressionNode left,
        VariantValue.Operation operation,
        QuantifierOperator quantifierOperator,
        SelectQueryExpressionBodyNode subQueryNode)
    {
        Left = left;
        Operation = operation;
        Operator = quantifierOperator;
        SubQueryNode = subQueryNode;
    }

    public SelectSubqueryConditionExpressionNode(SelectSubqueryConditionExpressionNode node)
        : this(
            (ExpressionNode)node.Left.Clone(),
            node.Operation,
            node.Operator,
            (SelectQueryExpressionBodyNode)node.SubQueryNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override string Code => "subquery_op";

    /// <inheritdoc />
    public override object Clone() => new SelectSubqueryConditionExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Left;
        yield return SubQueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
