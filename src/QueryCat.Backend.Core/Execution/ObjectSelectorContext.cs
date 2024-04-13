using System.Diagnostics;
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
    [DebuggerDisplay("{Object}, {PropertyInfo}")]
    public readonly record struct SelectInfo(object Object, PropertyInfo? PropertyInfo = null);

    /// <summary>
    /// Selector traverse stack.
    /// </summary>
    public Stack<SelectInfo> SelectStack { get; } = new();

    /// <summary>
    /// Optional user value.
    /// </summary>
    public object? Tag { get; set; }

    public ObjectSelectorContext()
    {
    }

    public ObjectSelectorContext(object startObject)
    {
        SelectStack.Push(new SelectInfo(startObject));
    }

    /// <summary>
    /// Reset state.
    /// </summary>
    public virtual void Clear()
    {
        SelectStack.Clear();
        Tag = null;
    }
}
