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
    /// <param name="typesToIgnore">Ignore node types.</param>
    /// <typeparam name="TNodeType">Node type to select.</typeparam>
    /// <returns>Enumerable of nodes.</returns>
    public static IEnumerable<TNodeType> GetAllChildren<TNodeType>(
        this IAstNode node,
        Type[]? typesToIgnore = null)
        where TNodeType : IAstNode
    {
        var stack = new Stack<IAstNode>();
        var currentNode = node;
        stack.Push(currentNode);
        typesToIgnore ??= Array.Empty<Type>();

        bool IsIgnoreType(Type type)
        {
            var foundIndex = Array.FindIndex(typesToIgnore, t => t.IsAssignableFrom(type));
            if (foundIndex > -1)
            {
                return true;
            }
            return false;
        }

        while (stack.Count > 0)
        {
            foreach (var childAstNode in currentNode.GetChildren())
            {
                if (!IsIgnoreType(childAstNode.GetType()))
                {
                    stack.Push(childAstNode);
                }
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
    /// Get required attribute assigned to the node.
    /// </summary>
    /// <param name="node">Node.</param>
    /// <param name="key">Attribute key.</param>
    /// <typeparam name="T">Expected attribute type.</typeparam>
    /// <returns>Attribute value.</returns>
    public static T GetRequiredAttribute<T>(this IAstNode node, string key)
    {
        var value = node.GetAttribute<T>(key);
        if (value == null)
        {
            throw new QueryCatException($"Cannot get attribute of type {typeof(T).Name} key '{key}' for node '{node}'.");
        }
        return value;
    }

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

    public static DataType GetDataType(this IAstNode node)
        => node.GetAttribute<DataType>(AstAttributeKeys.TypeKey);

    public static void SetDataType(this IAstNode node, DataType type)
        => node.SetAttribute(AstAttributeKeys.TypeKey, type);

    #endregion
}
