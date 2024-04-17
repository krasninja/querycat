using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Object selector context.
/// </summary>
public class ObjectSelectorContext
{
    /// <summary>
    /// Select object information.
    /// </summary>
    /// <param name="ResultObject">Object instance.</param>
    /// <param name="SelectProperty">Property information if the object is the property of another object.</param>
    /// <param name="Tag">Custom user object.</param>
    [DebuggerDisplay("{ResultObject}, {SelectProperty}")]
    public readonly record struct Token(
        object ResultObject,
        TokenPropertyInfo? SelectProperty = null,
        object? Tag = null);

    /// <summary>
    /// Select property information (if it is not a root object).
    /// </summary>
    /// <param name="Owner">Object the owner of the property.</param>
    /// <param name="PropertyInfo">Instance of <see cref="PropertyInfo" />.</param>
    [DebuggerDisplay("{Owner}, {PropertyInfo}")]
    public readonly record struct TokenPropertyInfo(
        object Owner,
        PropertyInfo PropertyInfo)
    {
        public static TokenPropertyInfo FromExpression<T>(T owner, Expression<Func<T, object>> expression)
            where T : class
        {
            return new TokenPropertyInfo(owner, GetPropertyInfo(expression));
        }
    }

    /// <summary>
    /// Selector traverse stack.
    /// </summary>
    public List<Token> SelectStack { get; } = new();

    /// <summary>
    /// Optional user value.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Previous result object.
    /// </summary>
    public object? PreviousResult => SelectStack.Count > 0 ? SelectStack[^1].ResultObject : null;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ObjectSelectorContext()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="startObject">Optional root object of the expression.</param>
    public ObjectSelectorContext(object startObject)
    {
        Push(new Token(startObject));
    }

    /// <summary>
    /// Push select info into stack.
    /// </summary>
    /// <param name="token">Select info.</param>
    public void Push(in Token token)
    {
        SelectStack.Add(token);
    }

    /// <summary>
    /// Pop select info from stack.
    /// </summary>
    /// <returns>Select info.</returns>
    public Token Pop()
    {
        var item = SelectStack[^1];
        SelectStack.RemoveAt(SelectStack.Count - 1);
        return item;
    }

    /// <summary>
    /// Peek last info.
    /// </summary>
    /// <returns>Select info.</returns>
    public Token Peek() => SelectStack[^1];

    /// <summary>
    /// Reset state.
    /// </summary>
    public virtual void Clear()
    {
        SelectStack.Clear();
        Tag = null;
    }

    private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object>> property)
    {
        LambdaExpression lambda = property;
        var memberExpression = lambda.Body is UnaryExpression expression
            ? (MemberExpression)expression.Operand
            : (MemberExpression)lambda.Body;

        return (PropertyInfo)memberExpression.Member;
    }
}
