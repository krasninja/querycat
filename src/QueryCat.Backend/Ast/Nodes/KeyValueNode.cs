namespace QueryCat.Backend.Ast.Nodes;

internal sealed class KeyValueNode : AstNode
{
    public LiteralNode Key { get; }

    public ExpressionNode Value { get; }

    /// <inheritdoc />
    public override string Code => "key_value";

    /// <inheritdoc />
    public KeyValueNode(LiteralNode key, ExpressionNode value)
    {
        Key = key;
        Value = value;
    }

    public KeyValueNode(KeyValueNode node) :
        this((LiteralNode)node.Key.Clone(), (ExpressionNode)node.Value.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return Key;
        yield return Value;
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override object Clone() => new KeyValueNode(this);
}
