namespace QueryCat.Backend.Ast.Nodes.Declare;

internal sealed class DeclareNode : AstNode
{
    public string Name { get; }

    public StatementNode? ValueNode { get; }

    /// <inheritdoc />
    public override string Code => "declare_var";

    public DeclareNode(string name, StatementNode? valueNode = null)
    {
        Name = name;
        ValueNode = valueNode;
    }

    public DeclareNode(DeclareNode node) : this(node.Name)
    {
        if (node.ValueNode != null)
        {
            ValueNode = (StatementNode)node.ValueNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new DeclareNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (ValueNode != null)
        {
            yield return ValueNode;
        }
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
