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
    /// <param name="ResultObject">Object instance.</param>
    /// <param name="SelectProperty">Property information if the object is the property of another object.</param>
    /// <param name="Tag">Custom user object.</param>
    [DebuggerDisplay("{ResultObject}, {SelectProperty}")]
    public readonly record struct SelectInfo(
        object ResultObject,
        SelectPropertyInfo? SelectProperty = null,
        object? Tag = null);

    public readonly record struct SelectPropertyInfo(object Owner, PropertyInfo PropertyInfo);

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
