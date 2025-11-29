namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWindowDefinitionListNode : AstNode
{
    public string Name { get; }

    public SelectWindowSpecificationNode WindowSpecificationNode { get; }

    /// <inheritdoc />
    public override string Code => "windowdef_list";

    /// <inheritdoc />
    public SelectWindowDefinitionListNode(string name, SelectWindowSpecificationNode windowSpecificationNode)
    {
        Name = name;
        WindowSpecificationNode = windowSpecificationNode;
    }

    public SelectWindowDefinitionListNode(SelectWindowDefinitionListNode node)
        : this(node.Name, (SelectWindowSpecificationNode)node.WindowSpecificationNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return WindowSpecificationNode;
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWindowDefinitionListNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
