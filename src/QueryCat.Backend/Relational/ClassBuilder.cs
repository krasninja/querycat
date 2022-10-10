using System.Linq.Expressions;
using System.Reflection;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Builder simplify the process of objects mapping to <see cref="RowsFrame" />.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public class ClassBuilder<TClass> where TClass : class
{
    private readonly List<(Column Column, Func<TClass, VariantValue> ValueGetter)> _columns = new();

    public ClassBuilder<TClass> AddProperty<T>(string name, Func<TClass, T> valueGetter, string? description = null)
    {
        var dataType = DataTypeUtils.ConvertFromSystem(typeof(T));
        _columns.Add((
            new Column(name, dataType, description),
            obj => VariantValue.CreateFromObject(valueGetter(obj))
        ));
        return this;
    }

    public ClassBuilder<TClass> AddProperty<T>(Expression<Func<TClass, T>> valueGetterExpression,
        string? description = null)
    {
        var name = GetPropertyName(valueGetterExpression);
        var dataType = DataTypeUtils.ConvertFromSystem(typeof(T));
        var valueGetter = valueGetterExpression.Compile();
        _columns.Add((
            new Column(name, dataType, description),
            obj => VariantValue.CreateFromObject(valueGetter.Invoke(obj))
        ));
        return this;
    }

    /// <summary>
    /// Build instance of <see cref="RowsFrame" />.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <returns>Instance of <see cref="ClassRowsFrame{TClass}" />.</returns>
    public ClassRowsFrame<TClass> BuildRowsFrame(RowsFrameOptions? options = null) => new(
        options ?? new RowsFrameOptions(),
        _columns.Select(c => c.Column).ToArray(),
        _columns.Select(c => c.ValueGetter).ToArray());

    /// <summary>
    /// Build iterator based on <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <param name="enumerable">Enumerable.</param>
    /// <returns>Enumerable iterator instance.</returns>
    public EnumerableRowsIterator<TClass> BuildIterator(IEnumerable<TClass> enumerable) => new(
        _columns.Select(c => c.Column).ToArray(),
        _columns.Select(c => c.ValueGetter).ToArray(),
        enumerable);

    /// <summary>
    /// Returns the name of the specified property of the specified type.
    /// </summary>
    /// <typeparam name="T1">The type the property is a member of.</typeparam>
    /// <typeparam name="T2">The return value type.</typeparam>
    /// <param name="property">The property.</param>
    /// <returns>The property name.</returns>
    private static string GetPropertyName<T1, T2>(Expression<Func<T1, T2>> property)
    {
        if (property.Body is UnaryExpression unaryExpression)
        {
            var memberExpression = (MemberExpression)unaryExpression.Operand;
            return ((PropertyInfo)memberExpression.Member).Name;
        }
        if (property.Body is MemberExpression bodyMemberExpression)
        {
            return ((PropertyInfo)bodyMemberExpression.Member).Name;
        }

        return string.Empty;
    }
}
