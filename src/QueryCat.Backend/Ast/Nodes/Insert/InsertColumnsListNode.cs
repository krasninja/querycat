namespace QueryCat.Backend.Ast.Nodes.Insert;

internal sealed class InsertColumnsListNode : AstNode
{
    public List<string> Columns { get; } = new();

    /// <inheritdoc />
    public override string Code => "columns_list";

    public InsertColumnsListNode()
    {
    }

    public InsertColumnsListNode(IEnumerable<string> columns) : this()
    {
        Columns.AddRange(columns);
    }

    public InsertColumnsListNode(InsertColumnsListNode node) : this(node.Columns)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new InsertColumnsListNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
