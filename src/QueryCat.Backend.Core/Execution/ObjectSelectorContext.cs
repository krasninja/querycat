using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Object selector context.
/// </summary>
[DebuggerDisplay("Count = {Length}")]
public sealed class ObjectSelectorContext
{
    private readonly IExecutionThread _executionThread;

    /// <summary>
    /// Running execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread => _executionThread;

    /// <summary>
    /// Select object information.
    /// </summary>
    /// <param name="ResultObject">Object instance.</param>
    /// <param name="PropertyInfo">Property information if the result object is the property of another object.
    /// Can only be defined for property selector.</param>
    /// <param name="Tag">Custom user object.</param>
    [DebuggerDisplay("{ResultObject}, {PropertyInfo}")]
    public readonly record struct Token(
        object ResultObject,
        PropertyInfo? PropertyInfo = null,
        object? Tag = null)
    {
        /// <summary>
        /// Create token from expression.
        /// </summary>
        /// <param name="owner">Owner object.</param>
        /// <param name="expression">Expression.</param>
        /// <typeparam name="T">Owner type.</typeparam>
        /// <returns>Instance of <see cref="Token" />.</returns>
        public static Token? From<T>(T owner, Expression<Func<T, object>> expression)
            where T : class
        {
            var pi = GetPropertyInfo(expression);
            var obj = pi.GetValue(owner);
            if (obj == null)
            {
                return null;
            }
            return new Token(obj, pi);
        }
    }

    private readonly List<Token> _selectStack = new();

    /// <summary>
    /// Selector traverse stack.
    /// </summary>
    public IReadOnlyList<Token> SelectStack => _selectStack;

    /// <summary>
    /// Select stack length.
    /// </summary>
    public int Length => SelectStack.Count;

    /// <summary>
    /// Previous result object.
    /// </summary>
    public object? PreviousResult => Length > 0 ? SelectStack[^1].ResultObject : null;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    public ObjectSelectorContext(IExecutionThread executionThread)
    {
        _executionThread = executionThread;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="startObject">Optional root object of the expression.</param>
    public ObjectSelectorContext(IExecutionThread executionThread, object startObject) : this(executionThread)
    {
        Push(new Token(startObject));
    }

    /// <summary>
    /// Push select info into stack.
    /// </summary>
    /// <param name="token">Select info.</param>
    public void Push(in Token token)
    {
        _selectStack.Add(token);
    }

    /// <summary>
    /// Pop select info from stack.
    /// </summary>
    /// <returns>Select info.</returns>
    public Token Pop()
    {
        var item = SelectStack[^1];
        _selectStack.RemoveAt(SelectStack.Count - 1);
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
    public void Clear()
    {
        _selectStack.Clear();
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
