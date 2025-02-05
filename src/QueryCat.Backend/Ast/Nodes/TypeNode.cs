using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Application type definition.
/// </summary>
internal class TypeNode : AstNode
{
    public DataType Type { get; }

    /// <inheritdoc />
    public override string Code => "type";

    /// <inheritdoc />
    public TypeNode(DataType type)
    {
        Type = type;
    }

    public TypeNode(TypeNode node) : this(node.Type)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new TypeNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => Type.ToString();
}
