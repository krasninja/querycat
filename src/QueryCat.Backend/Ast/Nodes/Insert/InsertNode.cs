using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast.Nodes.Insert;

public class InsertNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "insert";

    public FunctionCallNode InsertTargetNode { get; }

    public InsertColumnsListNode? ColumnsNode { get; set; }

    public SelectQueryNode QueryNode { get; }

    public InsertNode(FunctionCallNode insertTargetNode, SelectQueryNode queryNode)
    {
        InsertTargetNode = insertTargetNode;
        QueryNode = queryNode;
    }

    public InsertNode(InsertNode node)
        : this((FunctionCallNode)node.InsertTargetNode.Clone(), (SelectQueryNode)node.QueryNode.Clone())
    {
        if (node.ColumnsNode != null)
        {
            ColumnsNode = (InsertColumnsListNode)node.ColumnsNode.Clone();
        }
        node.CopyTo(this);
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
