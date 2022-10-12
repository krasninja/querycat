namespace QueryCat.Backend.Ast;

/// <summary>
/// Traverse AST using different methods.
/// </summary>
public class AstTraversal
{
    // Theory: https://www.geeksforgeeks.org/tree-traversals-inorder-preorder-and-postorder/

    private readonly AstVisitor _visitor;
    private readonly Stack<(IAstNode Node, IEnumerator<IAstNode> Enumerator)> _treeStack = new(32);

    public AstTraversal(AstVisitor visitor)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    /// <summary>
    /// Get current top parent.
    /// </summary>
    public IAstNode? CurrentParent => _treeStack.Count > 0 ? _treeStack.Peek().Node : null;

    /// <summary>
    /// Get current traversal stack values.
    /// </summary>
    /// <returns>Traversal stack values.</returns>
    public IEnumerable<IAstNode> GetCurrentStack() => _treeStack.Select(s => s.Node);

    /// <summary>
    /// Get first parent of type <see cref="TNode" />.
    /// </summary>
    /// <typeparam name="TNode">Target type.</typeparam>
    /// <returns>Found parent node or null.</returns>
    public TNode? GetFirstParent<TNode>() where TNode : IAstNode
        => _treeStack.Select(s => s.Node).OfType<TNode>().FirstOrDefault();

    /// <summary>
    /// Returns enumerable of all current node parents.
    /// </summary>
    /// <returns>Enumerable of parents.</returns>
    public IEnumerable<IAstNode> GetParents() => _treeStack.Select(s => s.Node);

    /// <summary>
    /// Pre-order traversal.
    /// </summary>
    /// <param name="node">Node to start the traversal.</param>
    public void PreOrder(IAstNode? node)
    {
        if (node == null)
        {
            return;
        }

        _treeStack.Push((node, node.GetChildren().GetEnumerator()));
        node.Accept(_visitor);
        while (_treeStack.Count > 0)
        {
            var current = _treeStack.Peek();
            if (current.Enumerator.MoveNext())
            {
                var next = current.Enumerator.Current;
                if (next == null!)
                {
                    continue;
                }
                _treeStack.Push((current.Enumerator.Current, next.GetChildren().GetEnumerator()));
                next.Accept(_visitor);
            }
            else
            {
                current.Enumerator.Dispose();
                _treeStack.Pop();
            }
        }
    }

    /// <summary>
    /// Post-order traversal.
    /// </summary>
    /// <param name="node">Node to start the traversal.</param>
    public void PostOrder(IAstNode? node)
    {
        if (node == null)
        {
            return;
        }

        _treeStack.Push((node, node.GetChildren().GetEnumerator()));
        while (_treeStack.Count > 0)
        {
            var current = _treeStack.Peek();
            if (current.Enumerator.MoveNext())
            {
                var next = current.Enumerator.Current;
                if (next == null!)
                {
                    continue;
                }
                _treeStack.Push((current.Enumerator.Current, next.GetChildren().GetEnumerator()));
            }
            else
            {
                current.Enumerator.Dispose();
                current.Node.Accept(_visitor);
                _treeStack.Pop();
            }
        }
    }
}
