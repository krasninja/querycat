using System.Text;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQuerySpecificationNode : AstNode
{
    public string Alias { get; internal set; } = string.Empty;

    public SelectQueryExpressionCombineNode? CombineQueryNode { get; private set; }

    /// <summary>
    /// CTE.
    /// </summary>
    public SelectWithListNode? WithNode { get; set; }

    /// <summary>
    /// Select columns list.
    /// </summary>
    public SelectColumnsListNode ColumnsListNode { get; }

    /// <summary>
    /// Distinct node.
    /// </summary>
    public SelectDistinctNode? DistinctNode { get; set; }

    /// <summary>
    /// "Into" SQL statement. Use default if null.
    /// </summary>
    public FunctionCallNode? TargetNode { get; set; }

    /// <summary>
    /// "From" SQL statement.
    /// </summary>
    public SelectTableExpressionNode? TableExpressionNode { get; set; }

    public SelectOrderByNode? OrderByNode { get; set; }

    public SelectOffsetNode? OffsetNode { get; set; }

    public SelectFetchNode? FetchNode { get; set; }

    /// <inheritdoc />
    public override string Code => "select_query";

    public SelectQuerySpecificationNode(SelectColumnsListNode columnsListNode)
    {
        ColumnsListNode = columnsListNode;
    }

    public SelectQuerySpecificationNode(SelectQuerySpecificationNode node) :
        this((SelectColumnsListNode)node.ColumnsListNode.Clone())
    {
        if (node.TargetNode != null)
        {
            TargetNode = (FunctionCallNode)node.TargetNode.Clone();
        }
        if (node.DistinctNode != null)
        {
            DistinctNode = (SelectDistinctNode)node.DistinctNode.Clone();
        }
        if (node.TableExpressionNode != null)
        {
            TableExpressionNode = (SelectTableExpressionNode)node.TableExpressionNode.Clone();
        }
        if (node.OffsetNode != null)
        {
            OffsetNode = (SelectOffsetNode)node.OffsetNode.Clone();
        }
        if (node.FetchNode != null)
        {
            FetchNode = (SelectFetchNode)node.FetchNode.Clone();
        }
        if (node.CombineQueryNode != null)
        {
            CombineQueryNode = (SelectQueryExpressionCombineNode)node.CombineQueryNode.Clone();
        }
        if (node.WithNode != null)
        {
            WithNode = (SelectWithListNode)node.WithNode.Clone();
        }
        node.CopyTo(this);
    }

    public void AppendCombineQuery(SelectQuerySpecificationNode node, SelectQueryExpressionCombineType combineType,
        bool isDistinct)
    {
        var next = this;
        while (next.CombineQueryNode != null)
        {
            next = next.CombineQueryNode.QueryNode;
        }
        next.CombineQueryNode = new SelectQueryExpressionCombineNode(node, combineType, isDistinct);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (WithNode != null)
        {
            yield return WithNode;
        }
        yield return ColumnsListNode;
        if (DistinctNode != null)
        {
            yield return DistinctNode;
        }
        if (TargetNode != null)
        {
            yield return TargetNode;
        }
        if (TableExpressionNode != null)
        {
            yield return TableExpressionNode;
        }
        if (OffsetNode != null)
        {
            yield return OffsetNode;
        }
        if (FetchNode != null)
        {
            yield return FetchNode;
        }
        if (OrderByNode != null)
        {
            yield return OrderByNode;
        }
        if (CombineQueryNode != null)
        {
            yield return CombineQueryNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQuerySpecificationNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (WithNode != null)
        {
            sb.Append($"With {WithNode}");
        }
        sb.Append(" Select");
        if (DistinctNode != null)
        {
            sb.Append($" {DistinctNode}");
        }
        foreach (var column in ColumnsListNode.Columns)
        {
            sb.Append($" {column}");
        }
        if (TargetNode != null)
        {
            sb.Append($" Into {TargetNode}");
        }
        if (TableExpressionNode != null)
        {
            sb.Append($" From {TableExpressionNode}");
        }
        if (OffsetNode != null)
        {
            sb.Append($" Offset {OffsetNode}");
        }
        if (FetchNode != null)
        {
            sb.Append($" Fetch {FetchNode}");
        }
        if (OrderByNode != null)
        {
            sb.Append($" Order {OrderByNode}");
        }
        if (CombineQueryNode != null)
        {
            sb.Append($" Combine {CombineQueryNode}");
        }
        return sb.ToString();
    }
}
