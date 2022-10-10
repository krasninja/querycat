using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Extensions for <see cref="AstNode" />.
/// </summary>
internal static class AstNodeExtensions
{
    /// <summary>
    /// Copy attribute from one node to another.
    /// </summary>
    /// <param name="fromNode">Source node.</param>
    /// <param name="key">Key to copy.</param>
    /// <param name="toNode">Destination node. </param>
    /// <typeparam name="T">Value type.</typeparam>
    public static void CopyTo<T>(this IAstNode fromNode, string key, IAstNode toNode)
    {
        var value = fromNode.GetAttribute<T>(key);
        toNode.SetAttribute(key, value);
    }

    /// <summary>
    /// Pre-order traverse the tree starting from the node
    /// and yield only nodes of type <see cref="TNodeType" />.
    /// </summary>
    /// <param name="node">AST node.</param>
    /// <typeparam name="TNodeType">Node type to select.</typeparam>
    /// <returns>Enumerable of nodes.</returns>
    public static IEnumerable<TNodeType> GetAllChildren<TNodeType>(
        this IAstNode node)
        where TNodeType : IAstNode
    {
        var stack = new Stack<IAstNode>();
        var currentNode = node;
        stack.Push(currentNode);

        while (stack.Count > 0)
        {
            foreach (var childAstNode in currentNode.GetChildren())
            {
                stack.Push(childAstNode);
            }
            currentNode = stack.Pop();
            if (currentNode is TNodeType foundNode)
            {
                yield return foundNode;
            }
        }
    }

    /// <summary>
    /// Determine if the node has specific attribute.
    /// </summary>
    /// <param name="node">Node.</param>
    /// <param name="key">Attribute key.</param>
    /// <returns><c>True</c> if attribute exists, <c>false</c> otherwise or it is null.</returns>
    public static bool HasAttribute(this IAstNode node, string key)
        => node.GetAttribute<object>(key) != null;

    /// <summary>
    /// Copy attribute value from one node to another.
    /// </summary>
    /// <param name="toNode">Destination node.</param>
    /// <param name="key">Attribute key.</param>
    /// <param name="fromNode">Source node.</param>
    public static void CopyAttributeFrom(this IAstNode toNode, string key, IAstNode fromNode)
    {
        if (fromNode.HasAttribute(key))
        {
            toNode.SetAttribute(key, fromNode.GetAttribute<object>(key));
        }
    }

    #region Attribute helpers

    public static Func<VariantValue> GetFunc(this IAstNode node)
        => node.GetAttribute<Func<VariantValue>>(Constants.FuncKey)
            ?? throw new InvalidOperationException("No function attribute.");

    public static void SetFunc(this IAstNode node, Func<VariantValue> func)
        => node.SetAttribute(Constants.FuncKey, func);

    public static DataType GetDataType(this IAstNode node)
        => node.GetAttribute<DataType>(Constants.TypeKey);

    public static void SetDataType(this IAstNode node, DataType type)
        => node.SetAttribute(Constants.TypeKey, type);

    #endregion
}
