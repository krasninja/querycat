namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// The node that does nothing.
/// </summary>
internal sealed class EmptyNode : AstNode
{
    /// <summary>
    /// Value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string Code => "empty";

    private readonly List<IAstNode> _children = new();

    public IReadOnlyList<IAstNode> Children => _children;

    public static EmptyNode Instance { get; } = new("(empty node)");

    public EmptyNode(string value, params IAstNode[] children)
    {
        Value = value;
        _children.AddRange(children);
    }

    public EmptyNode(EmptyNode node) : this(node.Value, node.Children.Select(c => (IAstNode)c.Clone()).ToArray())
    {
        node.CopyTo(this);
    }

    public IAstNode AppendChild(IAstNode childNode)
    {
        _children.Add(childNode);
        return childNode;
    }

    public EmptyNode AppendEmpty(string value, params EmptyNode[] children)
    {
        var node = new EmptyNode(value, children.Cast<IAstNode>().ToArray());
        _children.Add(node);
        return node;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Children;

    /// <inheritdoc />
    public override object Clone() => new EmptyNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => Value;
}
