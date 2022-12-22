namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectSubqueryExpressionNode : ExpressionNode
{
    public SelectQueryNode QueryNode { get; }

    /// <inheritdoc />
    public override string Code => "select_subquery";

    public SelectSubqueryExpressionNode(SelectQueryNode queryNode)
    {
        QueryNode = queryNode;
    }

    public SelectSubqueryExpressionNode(SelectSubqueryExpressionNode node)
        : this((SelectQueryNode)node.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectSubqueryExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return QueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
