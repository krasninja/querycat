using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQuerySpecificationNode : AstNode
{
    public SelectSetQuantifierNode QuantifierNode { get; set; } = new(false);

    public SelectColumnsListNode ColumnsList { get;  }

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
        yield return ColumnsList;
        if (Target != null)
        {
            yield return Target;
        }
        if (TableExpression != null)
        {
            yield return TableExpression;
        }
        if (OrderBy != null)
        {
            yield return OrderBy;
        }
        if (Offset != null)
        {
            yield return Offset;
        }
        if (Fetch != null)
        {
            yield return Fetch;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQuerySpecificationNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
