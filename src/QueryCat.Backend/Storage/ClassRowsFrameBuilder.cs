using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Builder simplify the process of objects mapping to <see cref="RowsFrame" />.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public class ClassRowsFrameBuilder<TClass> where TClass : class
{
    private const string DataColumn = "__data";

    private readonly List<(Column Column, Func<TClass, VariantValue> ValueGetter)> _columns = new();

    /// <summary>
    /// Property naming convention when used from expressions.
    /// </summary>
    public NamingConventionStyle NamingConvention { get; set; } = NamingConventionStyle.Keep;

    /// <summary>
    /// Columns.
    /// </summary>
    public IEnumerable<Column> Columns => _columns.Select(c => c.Column);

    /// <summary>
    /// Add data property that adds source object itself.
    /// </summary>
    /// <param name="description">Property description.</param>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddDataProperty(string? description = null)
    {
        var column = new Column(DataColumn, DataType.Object, description ?? "The raw data representation.");
        _columns.Add((
            column,
            obj => VariantValue.CreateFromObject(obj)
        ));
        return this;
    }

    /// <summary>
    /// Add property.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="valueGetter">The delegate to get property value by object.</param>
    /// <param name="description">Property description.</param>
    /// <param name="defaultLength">Default column length.</param>
    /// <typeparam name="T">Property type.</typeparam>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddProperty<T>(
        string name,
        Func<TClass, T> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = DataTypeUtils.ConvertFromSystem(typeof(T));
        var column = new Column(name, dataType, description);
        if (defaultLength.HasValue)
        {
            column.Length = defaultLength.Value;
        }
        _columns.Add((
            column,
            obj => VariantValue.CreateFromObject(valueGetter(obj))
        ));
        return this;
    }

    /// <summary>
    /// Add property from expression.
    /// </summary>
    /// <param name="valueGetterExpression">Property expression.</param>
    /// <param name="description">Property description.</param>
    /// <param name="defaultLength">Default column length.</param>
    /// <typeparam name="T">Property type.</typeparam>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddProperty<T>(
        Expression<Func<TClass, T>> valueGetterExpression,
        string? description = null,
        int? defaultLength = null)
    {
        var propertyInfo = GetProperty(valueGetterExpression);
        if (propertyInfo == null)
        {
            throw new InvalidOperationException("Cannot get column name from property.");
        }
        var propertyName = ConvertName(propertyInfo.Name);
        if (string.IsNullOrEmpty(description))
        {
            description = propertyInfo.GetCustomAttributes<DescriptionAttribute>()
                .Select(a => a.Description).FirstOrDefault();
        }
        var dataType = DataTypeUtils.ConvertFromSystem(typeof(T));
        var valueGetter = valueGetterExpression.Compile();
        var column = new Column(propertyName, dataType, description);
        if (defaultLength.HasValue)
        {
            column.Length = defaultLength.Value;
        }
        _columns.Add((
            column,
            obj => VariantValue.CreateFromObject(valueGetter.Invoke(obj))
        ));
        return this;
    }

    /// <summary>
    /// Build instance of <see cref="RowsFrame" />.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <returns>Instance of <see cref="ClassRowsFrame{TClass}" />.</returns>
    public ClassRowsFrame<TClass> BuildRowsFrame(RowsFrameOptions? options = null)
    {
        return new(
            options ?? new RowsFrameOptions(),
            _columns.Select(c => c.Column).ToArray(),
            _columns.Select(c => c.ValueGetter).ToArray());
    }

    /// <summary>
    /// Build iterator based on <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <param name="enumerable">Enumerable.</param>
    /// <returns>Enumerable iterator instance.</returns>
    public EnumerableRowsIterator<TClass> BuildIterator(IEnumerable<TClass> enumerable)
    {
        return new(
            _columns.Select(c => c.Column).ToArray(),
            _columns.Select(c => c.ValueGetter).ToArray(),
            enumerable);
    }

    /// <summary>
    /// Returns the name of the specified property of the specified type.
    /// </summary>
    /// <typeparam name="T1">The type the property is a member of.</typeparam>
    /// <typeparam name="T2">The return value type.</typeparam>
    /// <param name="property">The property.</param>
    /// <returns>The property name.</returns>
    private static PropertyInfo? GetProperty<T1, T2>(Expression<Func<T1, T2>> property)
    {
        if (property.Body is UnaryExpression unaryExpression)
        {
            var memberExpression = (MemberExpression)unaryExpression.Operand;
            return (PropertyInfo)memberExpression.Member;
        }
        if (property.Body is MemberExpression bodyMemberExpression)
        {
            return (PropertyInfo)bodyMemberExpression.Member;
        }

        return null;
    }

    /// <summary>
    /// Get value.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="obj">Object.</param>
    /// <returns>Value.</returns>
    public VariantValue GetValue(int columnIndex, TClass obj) => _columns[columnIndex].ValueGetter(obj);

    private string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (NamingConvention == NamingConventionStyle.CamelCase)
        {
            return name.Length < 2 ? name.ToLower() : char.ToLower(name[0]) + name[1..];
        }
        else if (NamingConvention == NamingConventionStyle.SnakeCase)
        {
            // Based on https://stackoverflow.com/questions/63055621/how-to-convert-camel-case-to-snake-case-with-two-capitals-next-to-each-other.
            var sb = new StringBuilder()
                .Append(char.ToLower(name[0]));
            for (var i = 1; i < name.Length; ++i)
            {
                var ch = name[i];
                if (char.IsUpper(ch))
                {
                    sb.Append('_');
                    sb.Append(char.ToLower(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
        else if (NamingConvention == NamingConventionStyle.PascalCase)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }

        return name;
    }
}
