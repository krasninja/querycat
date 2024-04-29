using System.Text;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectQuerySpecificationNode : SelectQueryNode
{
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

    /// <summary>
    /// Window node.
    /// </summary>
    public SelectWindowNode? WindowNode { get; set; }

    /// <inheritdoc />
    public override string Code => "select_query_spec";

    public SelectQuerySpecificationNode(SelectColumnsListNode columnsListNode)
        : base(columnsListNode)
    {
    }

    public SelectQuerySpecificationNode(SelectQuerySpecificationNode node) :
        base(node)
    {
        if (node.DistinctNode != null)
        {
            DistinctNode = (SelectDistinctNode)node.DistinctNode.Clone();
        }
        if (node.TargetNode != null)
        {
            TargetNode = (FunctionCallNode)node.TargetNode.Clone();
        }
        if (node.TableExpressionNode != null)
        {
            TableExpressionNode = (SelectTableExpressionNode)node.TableExpressionNode.Clone();
        }
        if (node.WindowNode != null)
        {
            WindowNode = (SelectWindowNode)node.WindowNode.Clone();
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
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
        if (WindowNode != null)
        {
            yield return WindowNode;
        }
        foreach (var astNode in base.GetChildren())
        {
            yield return astNode;
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
        sb.Append($" {string.Join(", ", ColumnsListNode.ColumnsNodes)}");
        if (TargetNode != null)
        {
            sb.Append($" Into {TargetNode}");
        }
        if (TableExpressionNode != null)
        {
            sb.Append($" From {TableExpressionNode}");
        }
        if (WindowNode != null)
        {
            sb.Append($" Window {WindowNode}");
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
        return sb.ToString();
    }
}
