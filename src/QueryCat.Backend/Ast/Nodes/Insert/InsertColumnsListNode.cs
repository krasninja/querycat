namespace QueryCat.Backend.Ast.Nodes.Insert;

public sealed class InsertColumnsListNode : AstNode
{
    public List<string> Columns { get; } = new();

    /// <inheritdoc />
    public override string Code => "columns_list";

    public InsertColumnsListNode(IEnumerable<string> columns)
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
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
