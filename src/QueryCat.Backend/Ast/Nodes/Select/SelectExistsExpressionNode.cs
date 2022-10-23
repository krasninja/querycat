namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectExistsExpressionNode : ExpressionNode
{
    public SelectQueryExpressionBodyNode SubQueryExpressionNode { get; }

    public SelectExistsExpressionNode(SelectQueryExpressionBodyNode subQueryExpressionNode)
    {
        SubQueryExpressionNode = subQueryExpressionNode;
    }

    public SelectExistsExpressionNode(SelectExistsExpressionNode node) :
        this((SelectQueryExpressionBodyNode)node.SubQueryExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override string Code => "exists";

    /// <inheritdoc />
    public override object Clone() => new SelectExistsExpressionNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return SubQueryExpressionNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
