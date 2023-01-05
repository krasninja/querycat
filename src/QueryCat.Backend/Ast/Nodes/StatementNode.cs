namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Program building block.
/// </summary>
public abstract class StatementNode : AstNode
{
    public IAstNode RootNode { get; }

    /// <summary>
    /// Next statement.
    /// </summary>
    public StatementNode? Next { get; set; }

    /// <inheritdoc />
    public override string Code => $"{RootNode.Code}_stmt";

    public StatementNode(IAstNode rootNode)
    {
        RootNode = rootNode;
    }

    public StatementNode(StatementNode node) : this((IAstNode)node.RootNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RootNode;
    }

    /// <inheritdoc />
    public override string ToString() => $"{RootNode}";
}
