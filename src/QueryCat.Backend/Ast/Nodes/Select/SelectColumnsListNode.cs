namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectColumnsListNode : AstNode
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

    /// <summary>
    /// Has defined columns. Returns false if there are no columns to select or "SELECT *" pattern.
    /// </summary>
    /// <returns>Returns <c>true</c> if select nodes has specific columns to select, <c>false</c> otherwise.</returns>
    public bool HasDefinedColumns()
    {
        if (ColumnsNodes.Count == 0)
        {
            return false;
        }
        if (ColumnsNodes.Count == 1 && ColumnsNodes[0] is SelectColumnsSublistAll)
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => ColumnsNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsListNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
