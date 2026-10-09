using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Object selector context.
/// </summary>
[DebuggerDisplay("Count = {Length}")]
public sealed class ObjectSelectorContext
{
    /// <summary>
    /// Running execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; internal set; } = NullExecutionThread.Instance;

    /// <summary>
    /// Select object information.
    /// </summary>
    /// <param name="Value">Result object instance.</param>
    /// <param name="PropertyInfo">Property information if the result object is the property of another object.
    /// Can only be defined for property selector.</param>
    /// <param name="Indexes">Indexes values if was selected by indexes.</param>
    /// <param name="Tag">Custom user object.</param>
    [DebuggerDisplay("{Value}, {PropertyInfo}")]
    public readonly record struct Token(
        object? Value,
        PropertyInfo? PropertyInfo = null,
        object?[]? Indexes = null,
        object? Tag = null)
    {
        /// <summary>
        /// Create token from expression.
        /// </summary>
        /// <param name="owner">Owner object.</param>
        /// <param name="expression">Expression.</param>
        /// <typeparam name="T">Owner type.</typeparam>
        /// <returns>Instance of <see cref="Token" />.</returns>
        public static Token? From<T>(T owner, Expression<Func<T, object?>> expression)
            where T : class
        {
            object? result;
            PropertyInfo? propertyInfo = null;

            // Value type properties are wrapped into Convert(...) to fit object return type.
            var body = expression.Body;
            while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            {
                body = unary.Operand;
            }

            // The compiler binds the property by the static type T (or by the virtual declaration for overrides),
            // so resolve it again using the runtime owner type.
            if (body is MemberExpression { Member: PropertyInfo declaredProperty } memberExpression
                && memberExpression.Expression == expression.Parameters[0])
            {
                propertyInfo = ResolveOwnerProperty(owner.GetType(), declaredProperty);
                result = propertyInfo.GetValue(owner);
            }
            else
            {
                result = expression.Compile().Invoke(owner);
            }

            if (result == null)
            {
                return null;
            }
            return new Token(result, propertyInfo);
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
    public object? LastValue => Length > 0 ? SelectStack[^1].Value : null;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal ObjectSelectorContext()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="startObject">Optional root object of the expression.</param>
    public ObjectSelectorContext(object startObject) : this()
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
    /// Trim stack to a specific length.
    /// </summary>
    /// <param name="targetLength">Target stack length.</param>
    public void Trim(int targetLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(targetLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(targetLength, Length);
        _selectStack.RemoveRange(targetLength, Length - targetLength);
    }

    /// <summary>
    /// Reset state.
    /// </summary>
    public void Clear()
    {
        _selectStack.Clear();
        ExecutionThread = NullExecutionThread.Instance;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Object selector works with arbitrary runtime objects whose properties cannot be statically annotated.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Object selector works with arbitrary runtime objects whose properties cannot be statically annotated.")]
    private static PropertyInfo ResolveOwnerProperty(Type runtimeType, PropertyInfo declaredProperty)
    {
        if (runtimeType == declaredProperty.DeclaringType)
        {
            return declaredProperty;
        }

        // Find the most derived declaration (override or "new" hidden property).
        // DeclaredOnly is used to avoid AmbiguousMatchException for hidden properties.
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                   | BindingFlags.DeclaredOnly;
        var indexParametersCount = declaredProperty.GetIndexParameters().Length;
        for (var type = runtimeType; type != null; type = type.BaseType)
        {
            foreach (var property in type.GetProperties(flags))
            {
                if (property.Name == declaredProperty.Name
                    && property.GetIndexParameters().Length == indexParametersCount)
                {
                    return property;
                }
            }
        }

        // For example, explicit interface implementation.
        return declaredProperty;
    }
}
