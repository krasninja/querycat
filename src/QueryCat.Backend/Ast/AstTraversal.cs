using System.Diagnostics;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Traverse AST using different methods.
/// </summary>
internal sealed class AstTraversal
{
    // Theory: https://www.geeksforgeeks.org/tree-traversals-inorder-preorder-and-postorder/

    private readonly AstVisitor _visitor;
    private readonly Stack<(IAstNode Node, IEnumerator<IAstNode> Enumerator)> _treeStack = new(32);

    /// <summary>
    /// The list of types the traversal will not visit.
    /// </summary>
    public List<Type> TypesToIgnore { get; } = new();

    /// <summary>
    /// If node type is within TypesToIgnore list it will be visited anyway, but children will not be traversed.
    /// </summary>
    public bool AcceptBeforeIgnore { get; set; }

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
    /// Get first parent of type <see ref="TNode" />.
    /// </summary>
    /// <typeparam name="TNode">Target type.</typeparam>
    /// <returns>Found parent node or null.</returns>
    public TNode? GetFirstParent<TNode>() where TNode : IAstNode
        => GetParents<TNode>().FirstOrDefault();

    /// <summary>
    /// Get first parent of type <see ref="TNode" /> that matches condition.
    /// </summary>
    /// <typeparam name="TNode">Target type.</typeparam>
    /// <returns>Found parent node or null.</returns>
    public TNode? GetFirstParent<TNode>(Func<IAstNode, bool> predicate) where TNode : IAstNode
        => GetParents().Where(predicate).OfType<TNode>().FirstOrDefault();

    /// <summary>
    /// Returns enumerable of all current node parents.
    /// </summary>
    /// <returns>Enumerable of parents.</returns>
    public IEnumerable<IAstNode> GetParents() => _treeStack.Select(s => s.Node);

    /// <summary>
    /// Returns enumerable of all current node parents.
    /// </summary>
    /// <returns>Enumerable of parents.</returns>
    public IEnumerable<TNode> GetParents<TNode>() where TNode : IAstNode
        => _treeStack.Skip(1).Select(s => s.Node).OfType<TNode>();

    /// <summary>
    /// Pre-order traversal.
    /// </summary>
    /// <param name="node">Node to start the traversal.</param>
    [DebuggerStepThrough]
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
                if (!IsIgnoreType(next.GetType()))
                {
                    _treeStack.Push((next, next.GetChildren().GetEnumerator()));
                    next.Accept(_visitor);
                }
                else if (AcceptBeforeIgnore)
                {
                    next.Accept(_visitor);
                }
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
    [DebuggerStepThrough]
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
                if (!IsIgnoreType(current.Enumerator.Current.GetType()))
                {
                    _treeStack.Push((next, next.GetChildren().GetEnumerator()));
                }
                else if (AcceptBeforeIgnore)
                {
                    next.Accept(_visitor);
                }
            }
            else
            {
                current.Enumerator.Dispose();
                current.Node.Accept(_visitor);
                _treeStack.Pop();
            }
        }
    }

    private bool IsIgnoreType(Type type)
    {
        foreach (var ignoreType in TypesToIgnore)
        {
            if (ignoreType.IsAssignableFrom(type))
            {
                return true;
            }
        }
        return false;
    }
}
