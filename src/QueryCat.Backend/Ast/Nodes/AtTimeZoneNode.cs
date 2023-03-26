namespace QueryCat.Backend.Ast.Nodes;

public sealed class AtTimeZoneNode : ExpressionNode
{
    public ExpressionNode LeftNode { get; }

    public ExpressionNode TimeZoneNode { get; }

    /// <inheritdoc />
    public override string Code => "at_tz";

    /// <inheritdoc />
    public AtTimeZoneNode(ExpressionNode leftNode, ExpressionNode timeZoneNode)
    {
        LeftNode = leftNode;
        TimeZoneNode = timeZoneNode;
    }

    public AtTimeZoneNode(AtTimeZoneNode node)
        : this((ExpressionNode)node.LeftNode.Clone(), (ExpressionNode)node.TimeZoneNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new AtTimeZoneNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return LeftNode;
        yield return TimeZoneNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
