namespace QueryCat.Backend.Ast.Nodes.Update;

internal sealed class UpdateSetNode : AstNode
{
    public IdentifierExpressionNode SetTargetNode { get; }

    public ExpressionNode SetSourceNode { get; }

    /// <inheritdoc />
    public override string Code => "update_set";

    /// <inheritdoc />
    public UpdateSetNode(IdentifierExpressionNode setTargetNode, ExpressionNode setSourceNode)
    {
        SetTargetNode = setTargetNode;
        SetSourceNode = setSourceNode;
    }

    public UpdateSetNode(UpdateSetNode node)
        : this((IdentifierExpressionNode)node.SetTargetNode.Clone(), (ExpressionNode)node.SetSourceNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new UpdateSetNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return SetTargetNode;
        yield return SetSourceNode;
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
