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
        node.CopyTo(this);
    }

    internal static void FixNextNodeLink(IReadOnlyList<StatementNode> statements)
    {
        for (var i = 0; i < statements.Count; i++)
        {
            statements[i].NextNode = i + 1 < statements.Count ? statements[i + 1] : null;
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return RootNode;
        // We don't need to iterate over the next node, because we process one statement at once.
    }

    /// <inheritdoc />
    public override string ToString() => $"{RootNode}";
}
