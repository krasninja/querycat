namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsListNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "columns_list";

    public List<SelectColumnsSublistNode> Columns { get; }

    public static SelectColumnsListNode Empty { get; } = new(Array.Empty<SelectColumnsSublistNode>());

    public SelectColumnsListNode(List<SelectColumnsSublistNode> columns)
    {
        Columns = columns;
    }

    public SelectColumnsListNode(params SelectColumnsSublistNode[] columns)
    {
        Columns = new List<SelectColumnsSublistNode>(columns);
    }

    public SelectColumnsListNode(SelectColumnsListNode node) :
        this(node.Columns.Select(c => (SelectColumnsSublistNode)c.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Columns;

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsListNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
