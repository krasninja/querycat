using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast.Nodes.Insert;

public class InsertNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "insert";

    public ExpressionNode InsertTargetNode { get; }

    public InsertColumnsListNode? ColumnsNode { get; set; }

    public SelectQueryNode QueryNode { get; }

    public InsertNode(ExpressionNode insertTargetNode, SelectQueryNode queryNode)
    {
        InsertTargetNode = insertTargetNode;
        QueryNode = queryNode;
    }

    public InsertNode(InsertNode node)
        : this((ExpressionNode)node.InsertTargetNode.Clone(), (SelectQueryNode)node.QueryNode.Clone())
    {
        if (node.ColumnsNode != null)
        {
            ColumnsNode = (InsertColumnsListNode)node.ColumnsNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <summary>
    /// Has defined columns. Returns false if there are no predefined columns in the source.
    /// </summary>
    /// <returns>Returns <c>true</c> if select nodes has specific columns to select, <c>false</c> otherwise.</returns>
    public bool HasDefinedColumns()
    {
        return ColumnsNode != null && ColumnsNode.Columns.Count > 0;
    }

    /// <inheritdoc />
    public override object Clone() => new InsertNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return InsertTargetNode;
        if (ColumnsNode != null)
        {
            yield return ColumnsNode;
        }
        yield return QueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
