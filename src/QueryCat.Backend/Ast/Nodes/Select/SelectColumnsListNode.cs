namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectColumnsListNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "columns_list";

    public List<SelectColumnsSublistNode> ColumnsNodes { get; } = new();

    public SelectColumnsListNode(IEnumerable<SelectColumnsSublistNode> columnsNodes)
    {
        ColumnsNodes.AddRange(columnsNodes);
    }

    public SelectColumnsListNode(params SelectColumnsSublistNode[] columns)
    {
        ColumnsNodes = new List<SelectColumnsSublistNode>(columns);
    }

    public SelectColumnsListNode(SelectColumnsListNode node) :
        this(node.ColumnsNodes.Select(c => (SelectColumnsSublistNode)c.Clone()))
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

    internal IReadOnlyList<string> GetColumnsNames()
    {
        var list = new List<string>();
        foreach (var column in ColumnsNodes)
        {
            if (!string.IsNullOrEmpty(column.Alias))
            {
                list.Add(column.Alias);
                continue;
            }
            if (column is SelectColumnsSublistExpressionNode expressionNode
                && expressionNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                list.Add(identifierExpressionNode.TableFullName);
                continue;
            }
        }
        return list;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => ColumnsNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsListNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
