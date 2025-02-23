namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWithListNode : AstNode
{
    public List<SelectWithNode> WithNodes { get; } = new();

    public bool IsRecursive { get; }

    /// <inheritdoc />
    public override string Code => "with";

    public SelectWithListNode(IEnumerable<SelectWithNode> withNodes, bool isRecursive)
    {
        WithNodes.AddRange(withNodes);
        IsRecursive = isRecursive;
    }

    public SelectWithListNode(SelectWithListNode node)
        : this(node.WithNodes.Select(c => (SelectWithNode)c.Clone()).ToList(), node.IsRecursive)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWithListNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var withNode in WithNodes)
        {
            yield return withNode;
        }
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => string.Join(", ", WithNodes.Select(n => n.ToString()));
}
