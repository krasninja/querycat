namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsListNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "columns_list";

    public List<SelectColumnsSublistNode> ColumnsNodes { get; } = new();

    public static SelectColumnsListNode Empty { get; } = new(Array.Empty<SelectColumnsSublistNode>());

    public SelectColumnsListNode(IEnumerable<SelectColumnsSublistNode> columnsNodes)
    {
        ColumnsNodes.AddRange(columnsNodes);
    }

    public SelectColumnsListNode(params SelectColumnsSublistNode[] columns)
    {
        ColumnsNodes = new List<SelectColumnsSublistNode>(columns);
    }

    public SelectColumnsListNode(SelectColumnsListNode node) :
        this(node.ColumnsNodes.Select(c => (SelectColumnsSublistNode)c.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => ColumnsNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsListNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
