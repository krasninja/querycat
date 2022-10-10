namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectHavingNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "having";

    public ExpressionNode ExpressionNode { get; }

    public SelectHavingNode(ExpressionNode expressionNode)
    {
        ExpressionNode = expressionNode;
    }

    public SelectHavingNode(SelectHavingNode node) :
        this((ExpressionNode)node.ExpressionNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectHavingNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
