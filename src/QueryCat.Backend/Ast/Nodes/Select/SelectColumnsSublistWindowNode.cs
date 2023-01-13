namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsSublistWindowNode : SelectColumnsSublistNode
{
    public SelectWindowSpecificationNode WindowSpecificationNode { get; }

    /// <inheritdoc />
    public override string Code => "column_window";

    /// <inheritdoc />
    public SelectColumnsSublistWindowNode(SelectWindowSpecificationNode windowSpecificationNode)
    {
        WindowSpecificationNode = windowSpecificationNode;
    }

    public SelectColumnsSublistWindowNode(SelectColumnsSublistWindowNode node)
        : this((SelectWindowSpecificationNode)node.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return WindowSpecificationNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistWindowNode(this);
}
