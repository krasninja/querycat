namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectWindowSpecificationNode : AstNode
{
    public string ExistingWindowName { get; }

    public SelectWindowPartitionClauseNode? PartitionNode { get; }

    public SelectWindowOrderClauseNode? OrderNode { get; }

    /// <inheritdoc />
    public override string Code => "windowspec";

    /// <inheritdoc />
    public SelectWindowSpecificationNode(SelectWindowPartitionClauseNode? partitionNode, SelectWindowOrderClauseNode? orderNode)
        : this(string.Empty, partitionNode, orderNode)
    {
    }

    /// <inheritdoc />
    public SelectWindowSpecificationNode(
        string existingWindowName,
        SelectWindowPartitionClauseNode? partitionNode,
        SelectWindowOrderClauseNode? orderNode)
    {
        ExistingWindowName = existingWindowName;
        PartitionNode = partitionNode;
        OrderNode = orderNode;
    }

    public SelectWindowSpecificationNode(SelectWindowSpecificationNode node)
    {
        ExistingWindowName = node.ExistingWindowName;
        if (node.PartitionNode != null)
        {
            PartitionNode = (SelectWindowPartitionClauseNode)node.PartitionNode.Clone();
        }
        if (node.OrderNode != null)
        {
            OrderNode = (SelectWindowOrderClauseNode)node.OrderNode.Clone();
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (PartitionNode != null)
        {
            yield return PartitionNode;
        }
        if (OrderNode != null)
        {
            yield return OrderNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectWindowSpecificationNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
