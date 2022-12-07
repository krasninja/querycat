using System.Text;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQuerySpecificationNode : AstNode
{
    /// <summary>
    /// CTE.
    /// </summary>
    public SelectWithListNode? With { get; set; }

    /// <summary>
    /// Select columns list.
    /// </summary>
    public SelectColumnsListNode ColumnsList { get; }

    /// <summary>
    /// Distinct node.
    /// </summary>
    public SelectDistinctNode? Distinct { get; set; }

    /// <summary>
    /// "Into" SQL statement. Use default if null.
    /// </summary>
    public FunctionCallNode? Target { get; set; }

    /// <summary>
    /// "From" SQL statement.
    /// </summary>
    public SelectTableExpressionNode? TableExpression { get; set; }

    public SelectOrderByNode? OrderBy { get; set; }

    public SelectOffsetNode? Offset { get; set; }

    public SelectFetchNode? Fetch { get; set; }

    /// <inheritdoc />
    public override string Code => "select_query";

    public SelectQuerySpecificationNode(SelectColumnsListNode columnsList)
    {
        ColumnsList = columnsList;
    }

    public SelectQuerySpecificationNode(SelectQuerySpecificationNode node) :
        this((SelectColumnsListNode)node.ColumnsList.Clone())
    {
        if (node.Target != null)
        {
            Target = (FunctionCallNode)node.Target.Clone();
        }
        if (node.Distinct != null)
        {
            Distinct = (SelectDistinctNode)node.Distinct.Clone();
        }
        if (node.TableExpression != null)
        {
            TableExpression = (SelectTableExpressionNode)node.TableExpression.Clone();
        }
        if (node.Offset != null)
        {
            Offset = (SelectOffsetNode)node.Offset.Clone();
        }
        if (node.Fetch != null)
        {
            Fetch = (SelectFetchNode)node.Fetch.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (With != null)
        {
            yield return With;
        }
        yield return ColumnsList;
        if (Distinct != null)
        {
            yield return Distinct;
        }
        if (Target != null)
        {
            yield return Target;
        }
        if (TableExpression != null)
        {
            yield return TableExpression;
        }
        if (Offset != null)
        {
            yield return Offset;
        }
        if (Fetch != null)
        {
            yield return Fetch;
        }
        if (OrderBy != null)
        {
            yield return OrderBy;
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
        sb.Append("Select");
        if (Distinct != null)
        {
            sb.Append($" {Distinct}");
        }
        foreach (var column in ColumnsList.Columns)
        {
            sb.Append($" {column}");
        }
        if (Target != null)
        {
            sb.Append($" Into {Target}");
        }
        if (TableExpression != null)
        {
            sb.Append($" From {TableExpression}");
        }
        if (Offset != null)
        {
            sb.Append($" Offset {Offset}");
        }
        if (Fetch != null)
        {
            sb.Append($" Fetch {Fetch}");
        }
        if (OrderBy != null)
        {
            sb.Append($" Order {OrderBy}");
        }
        return sb.ToString();
    }
}
