namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectWindowSpecificationNode : AstNode
{
    public string ExistingWindowName { get; } = string.Empty;

    public SelectWindowPartitionClauseNode? PartitionNode { get; internal set; }

    public SelectWindowOrderClauseNode? OrderNode { get; }

    /// <inheritdoc />
    public override string Code => "windowspec";

    public SelectWindowSpecificationNode(string existingWindowName)
    {
        ExistingWindowName = existingWindowName;
    }

    /// <inheritdoc />
    public SelectWindowSpecificationNode(SelectWindowPartitionClauseNode? partitionNode, SelectWindowOrderClauseNode? orderNode)
    {
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
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
