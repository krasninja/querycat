using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Traverse AST using different methods.
/// </summary>
internal sealed class AstTraversal
{
    // Theory: https://www.geeksforgeeks.org/tree-traversals-inorder-preorder-and-postorder/

    private readonly AstVisitor _visitor;

    private readonly struct TraversalItem : IDisposable
    {
        public IAstNode Node { get; }

        public IEnumerator<IAstNode> ChildrenEnumerator { get; }

        public IAstNode CurrentChild => ChildrenEnumerator.Current;

        public TraversalItem(IAstNode node)
        {
            Node = node;
            ChildrenEnumerator = node.GetChildren().GetEnumerator();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ChildrenEnumerator.Dispose();
        }

        /// <inheritdoc />
        public override string ToString() => Node.ToString() ?? string.Empty;
    }

    private readonly Stack<TraversalItem> _traversalStack = new(32);

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
        ArgumentNullException.ThrowIfNull(visitor, nameof(visitor));
        _visitor = visitor;
    }

    /// <summary>
    /// Get current top parent.
    /// </summary>
    public IAstNode? CurrentParent => _traversalStack.Count > 0 ? _traversalStack.Peek().Node : null;

    /// <summary>
    /// Get current traversal stack values.
    /// </summary>
    /// <returns>Traversal stack values.</returns>
    public IEnumerable<IAstNode> GetCurrentStack() => _traversalStack.Select(s => s.Node);

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
    public IEnumerable<IAstNode> GetParents() => _traversalStack.Select(s => s.Node);

    /// <summary>
    /// Returns enumerable of all current node parents.
    /// </summary>
    /// <returns>Enumerable of parents.</returns>
    public IEnumerable<TNode> GetParents<TNode>() where TNode : IAstNode
        => _traversalStack.Skip(1).Select(s => s.Node).OfType<TNode>();

    /// <summary>
    /// Pre-order traversal, async version.
    /// </summary>
    /// <param name="node">Node to start the traversal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [DebuggerStepThrough]
    public async ValueTask PreOrderAsync(IAstNode? node, CancellationToken cancellationToken)
    {
        if (node == null)
        {
            return;
        }
        var ignoreTypes = TypesToIgnore.ToArray();

        try
        {
            _traversalStack.Push(new TraversalItem(node));
            await node.AcceptAsync(_visitor, cancellationToken);
            while (_traversalStack.Count > 0)
            {
                var current = _traversalStack.Peek();
                if (current.ChildrenEnumerator.MoveNext())
                {
                    var next = current.CurrentChild;
                    if (!IsIgnoreType(next.GetType(), ignoreTypes))
                    {
                        _traversalStack.Push(new TraversalItem(next));
                        await next.AcceptAsync(_visitor, cancellationToken);
                    }
                    else if (AcceptBeforeIgnore)
                    {
                        await next.AcceptAsync(_visitor, cancellationToken);
                    }
                }
                else
                {
                    _traversalStack.Pop();
                }
            }
        }
        finally
        {
            _traversalStack.Clear();
        }
    }

    /// <summary>
    /// Post-order traversal, async version.
    /// </summary>
    /// <param name="node">Node to start the traversal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [DebuggerStepThrough]
    public async ValueTask PostOrderAsync(IAstNode? node, CancellationToken cancellationToken)
    {
        if (node == null)
        {
            return;
        }
        var ignoreTypes = TypesToIgnore.ToArray();

        try
        {
            _traversalStack.Push(new TraversalItem(node));
            while (_traversalStack.Count > 0)
            {
                var current = _traversalStack.Peek();
                if (current.ChildrenEnumerator.MoveNext())
                {
                    var next = current.CurrentChild;
                    if (!IsIgnoreType(current.CurrentChild.GetType(), ignoreTypes))
                    {
                        _traversalStack.Push(new TraversalItem(next));
                    }
                    else if (AcceptBeforeIgnore)
                    {
                        await next.AcceptAsync(_visitor, cancellationToken);
                    }
                }
                else
                {
                    await current.Node.AcceptAsync(_visitor, cancellationToken);
                    _traversalStack.Pop();
                }
            }
        }
        finally
        {
            _traversalStack.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool IsIgnoreType(Type type, Type[] ignoreTypes)
    {
        foreach (var ignoreType in ignoreTypes)
        {
            if (ignoreType == type)
            {
                return true;
            }
        }
        return false;
    }
}
