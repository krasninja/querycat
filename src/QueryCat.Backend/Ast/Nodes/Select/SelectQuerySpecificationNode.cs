using System.Text;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQuerySpecificationNode : SelectQueryNode
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

    /// <inheritdoc />
    public override string Code => "select_query_spec";

    public SelectQuerySpecificationNode(SelectColumnsListNode columnsListNode)
        : base(columnsListNode)
    {
    }

    public SelectQuerySpecificationNode(SelectQuerySpecificationNode node) :
        base(node)
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
        return sb.ToString();
    }
}
