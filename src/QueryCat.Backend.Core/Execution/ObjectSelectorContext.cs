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
    public List<SelectInfo> SelectStack { get; } = new();

    /// <summary>
    /// Optional user value.
    /// </summary>
    public object? Tag { get; set; }

    public ObjectSelectorContext()
    {
    }

    public ObjectSelectorContext(object startObject)
    {
        Push(new SelectInfo(startObject));
    }

    /// <summary>
    /// Push select info into stack.
    /// </summary>
    /// <param name="selectInfo">Select info.</param>
    public void Push(in SelectInfo selectInfo)
    {
        SelectStack.Add(selectInfo);
    }

    /// <summary>
    /// Pop select info from stack.
    /// </summary>
    /// <returns>Select info.</returns>
    public SelectInfo Pop()
    {
        var item = SelectStack[^1];
        SelectStack.RemoveAt(SelectStack.Count - 1);
        return item;
    }

    /// <summary>
    /// Peek last info.
    /// </summary>
    /// <returns>Select info.</returns>
    public SelectInfo Peek() => SelectStack[^1];

    /// <summary>
    /// Reset state.
    /// </summary>
    public virtual void Clear()
    {
        SelectStack.Clear();
        Tag = null;
    }
}
