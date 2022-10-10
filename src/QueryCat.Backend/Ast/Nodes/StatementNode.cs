namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Program building block.
/// </summary>
public abstract class StatementNode : AstNode
{
    /// <summary>
    /// Next statement.
    /// </summary>
    public StatementNode? Next { get; set; }
}
