namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Program building block.
/// </summary>
internal abstract class StatementNode : AstNode
{
    public IAstNode RootNode { get; }

    /// <summary>
    /// Next statement.
    /// </summary>
    public StatementNode? NextNode { get; set; }

    /// <inheritdoc />
    public override string Code => $"{RootNode.Code}_stmt";

    public StatementNode(IAstNode rootNode)
    {
        RootNode = rootNode;
    }

    public StatementNode(StatementNode node) : this((IAstNode)node.RootNode.Clone())
    {
        if (node.NextNode != null)
        {
            NextNode = (StatementNode)node.NextNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RootNode;
        if (NextNode != null)
        {
            yield return NextNode;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{RootNode}";
}
