namespace QueryCat.Backend.Ast.Nodes.For;

internal sealed class ForNode : AstNode
{
    public string TargetVariableName { get; }

    public ExpressionNode QueryExpression { get; }

    public ProgramBodyNode ProgramBodyNode { get; }

    /// <inheritdoc />
    public override string Code => "for";

    /// <inheritdoc />
    public ForNode(
        string targetVariableName,
        ExpressionNode queryExpression,
        ProgramBodyNode programBodyNode)
    {
        TargetVariableName = targetVariableName;
        QueryExpression = queryExpression;
        ProgramBodyNode = programBodyNode;
    }

    public ForNode(ForNode node) : this(
        node.TargetVariableName,
        (ExpressionNode)node.QueryExpression.Clone(),
        (ProgramBodyNode)node.ProgramBodyNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new ForNode(TargetVariableName, QueryExpression, ProgramBodyNode);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return QueryExpression;
        yield return ProgramBodyNode;
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
