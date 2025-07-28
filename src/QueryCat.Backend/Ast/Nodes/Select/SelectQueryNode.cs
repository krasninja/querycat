using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal abstract class SelectQueryNode : ExpressionNode, ISelectAliasNode
{
    /// <inheritdoc />
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// CTE.
    /// </summary>
    public SelectWithListNode? WithNode { get; set; }

    /// <summary>
    /// Select columns list.
    /// </summary>
    public SelectColumnsListNode ColumnsListNode { get; }

    /// <summary>
    /// Identifiers that should not be shown.
    /// </summary>
    public SelectColumnsExceptNode? ExceptIdentifiersNode { get; set; }

    /// <summary>
    /// Sort node.
    /// </summary>
    public SelectOrderByNode? OrderByNode { get; set; }

    /// <summary>
    /// Number of rows to offset node.
    /// </summary>
    public SelectOffsetNode? OffsetNode { get; set; }

    /// <summary>
    /// Number of rows to limit node.
    /// </summary>
    public SelectFetchNode? FetchNode { get; set; }

    public SelectQueryNode(SelectColumnsListNode columnsListNode)
    {
        ColumnsListNode = columnsListNode;
    }

    public SelectQueryNode(SelectQueryNode node) : this((SelectColumnsListNode)node.ColumnsListNode.Clone())
    {
        Alias = node.Alias;
        if (node.ExceptIdentifiersNode != null)
        {
            ExceptIdentifiersNode = (SelectColumnsExceptNode)node.ExceptIdentifiersNode.Clone();
        }
        if (node.WithNode != null)
        {
            WithNode = (SelectWithListNode)node.WithNode.Clone();
        }
        if (node.OrderByNode != null)
        {
            OrderByNode = (SelectOrderByNode)node.OrderByNode.Clone();
        }
        if (node.OffsetNode != null)
        {
            OffsetNode = (SelectOffsetNode)node.OffsetNode.Clone();
        }
        if (node.FetchNode != null)
        {
            FetchNode = (SelectFetchNode)node.FetchNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <summary>
    /// Does the query returns only single value.
    /// </summary>
    public virtual bool IsSingleValue()
    {
        if (ColumnsListNode.ColumnsNodes.Count == 1
               && FetchNode?.CountNode is LiteralNode literalNode
               && literalNode.Value.Type == DataType.Integer
               && literalNode.Value.AsInteger == 1)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ColumnsListNode;
        if (ExceptIdentifiersNode != null)
        {
            yield return ExceptIdentifiersNode;
        }
        if (WithNode != null)
        {
            yield return WithNode;
        }
        if (OrderByNode != null)
        {
            yield return OrderByNode;
        }
        if (OffsetNode != null)
        {
            yield return OffsetNode;
        }
        if (FetchNode != null)
        {
            yield return FetchNode;
        }
    }
}
