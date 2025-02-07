using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes.Declare;

internal sealed class DeclareNode : AstNode
{
    public string Name { get; }

    public DataType Type { get; }

    public StatementNode? ValueNode { get; }

    /// <inheritdoc />
    public override string Code => "declare_var";

    public DeclareNode(string name, DataType type, StatementNode? valueNode = null)
    {
        Name = name;
        Type = type;
        ValueNode = valueNode;
    }

    public DeclareNode(DeclareNode node) : this(node.Name, node.Type)
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
