namespace QueryCat.Backend.Ast.Nodes.If;

internal sealed class IfConditionNode : AstNode
{
    /// <inheritdoc />
    public override string Code => "if";

    public List<IfConditionItemNode> ConditionsList { get; } = new();

    public BlockExpressionNode? ElseNode { get; }

    /// <inheritdoc />
    public IfConditionNode(IList<IfConditionItemNode> conditionBlocks, BlockExpressionNode? elseNode = null)
    {
        if (conditionBlocks.Count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(conditionBlocks), Resources.Errors.NoConditions);
        }
        ConditionsList.AddRange(conditionBlocks);
        ElseNode = elseNode;
    }

    public IfConditionNode(IfConditionNode node)
        : this(node.ConditionsList.Select(c => (IfConditionItemNode)c.Clone()).ToList())
    {
        if (node.ElseNode != null)
        {
            ElseNode = (BlockExpressionNode)node.ElseNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var itemNode in ConditionsList)
        {
            yield return itemNode;
        }
        if (ElseNode != null)
        {
            yield return ElseNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new IfConditionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);
}
