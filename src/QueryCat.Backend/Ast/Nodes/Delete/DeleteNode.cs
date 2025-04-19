using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast.Nodes.Delete;

internal sealed class DeleteNode : AstNode
{
    public ExpressionNode DeleteTargetNode { get; }

    public SelectSearchConditionNode? SearchConditionNode { get; }

    /// <inheritdoc />
    public override string Code => "delete";

    public DeleteNode(ExpressionNode deleteTargetNode, SelectSearchConditionNode? searchConditionNode)
    {
        DeleteTargetNode = deleteTargetNode;
        SearchConditionNode = searchConditionNode;
    }

    public DeleteNode(DeleteNode node) : this((ExpressionNode)node.DeleteTargetNode.Clone(), null)
    {
        if (node.SearchConditionNode != null)
        {
            SearchConditionNode = (SelectSearchConditionNode)node.SearchConditionNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return DeleteTargetNode;
        if (SearchConditionNode != null)
        {
            yield return SearchConditionNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new DeleteNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
