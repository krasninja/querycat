using System.Reflection;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Object selector context.
/// </summary>
public class ObjectSelectorContext
{
    /// <summary>
    /// Object information.
    /// </summary>
    /// <param name="Object">Object instance.</param>
    /// <param name="PropertyInfo">Property information if the object is the property of another object.</param>
    public readonly record struct ObjectInfo(object Object, PropertyInfo? PropertyInfo = null);

    /// <summary>
    /// Selector traverse stack.
    /// </summary>
    public Stack<ObjectInfo> SelectStack { get; } = new();

    /// <summary>
    /// Optional user value.
    /// </summary>
    public object? Tag { get; set; }

    public ObjectSelectorContext(object startObject)
    {
        SelectStack.Push(new ObjectInfo(startObject));
    }
}
