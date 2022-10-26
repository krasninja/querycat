namespace QueryCat.Backend.Ast;

/// <summary>
/// The helper class that helps map child-parent between AST nodes.
/// </summary>
internal sealed class AstIndex
{
    private readonly Dictionary<IAstNode, IAstNode?> _childParentMap = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="root">The root node to start create index.</param>
    public AstIndex(IAstNode root)
    {
        var stack = new Stack<IAstNode>();
        var currentNode = root;
        stack.Push(currentNode);

        while (stack.Count > 0)
        {
            foreach (var childAstNode in currentNode.GetChildren())
            {
                stack.Push(childAstNode);
            }
            currentNode = stack.Pop();
            _childParentMap[currentNode] = stack.Count > 0 ? stack.Peek() : null;
        }
    }

    /// <summary>
    /// Get parent node.
    /// </summary>
    /// <param name="child">Child node.</param>
    /// <returns>Parent node or null.</returns>
    public IAstNode? GetParent(IAstNode child) => _childParentMap[child];
}
