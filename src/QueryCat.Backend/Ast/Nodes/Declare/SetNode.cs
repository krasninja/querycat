namespace QueryCat.Backend.Ast.Nodes.Declare;

internal sealed class SetNode : AstNode
{
    public string Name { get; }

    public StatementNode ValueNode { get; }

    /// <inheritdoc />
    public override string Code => "set_var";

    public SetNode(string name, StatementNode valueNode)
    {
        Name = name;
        ValueNode = valueNode;
    }

    public SetNode(SetNode node) : this(node.Name, (StatementNode)node.ValueNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SetNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ValueNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
