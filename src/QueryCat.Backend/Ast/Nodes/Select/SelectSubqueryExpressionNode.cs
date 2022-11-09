namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectSubqueryExpressionNode : ExpressionNode
{
    public SelectQueryExpressionBodyNode QueryExpressionBodyNode { get; }

    /// <inheritdoc />
    public override string Code => "select_subquery";

    public SelectSubqueryExpressionNode(SelectQueryExpressionBodyNode queryExpressionBodyNode)
    {
        QueryExpressionBodyNode = queryExpressionBodyNode;
    }

    public SelectSubqueryExpressionNode(SelectSubqueryExpressionNode node)
        : this((SelectQueryExpressionBodyNode)node.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectSubqueryExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return QueryExpressionBodyNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
