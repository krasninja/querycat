using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Builder simplify the process of objects mapping to rows frame.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public class ClassRowsFrameBuilder<TClass> where TClass : class
{
    private const string DataColumn = "__data";

    private readonly List<(Data.Column Column, Func<TClass, VariantValue> ValueGetter)> _columns = new();

    /// <summary>
    /// Property naming convention when used from expressions.
    /// </summary>
    public NamingConventionStyle NamingConvention { get; set; } = NamingConventionStyle.Keep;

    /// <summary>
    /// Columns.
    /// </summary>
    public IEnumerable<Column> Columns => _columns.Select(c => c.Column);

    /// <summary>
    /// Columns.
    /// </summary>
    public IEnumerable<Func<TClass, VariantValue>> Getters => _columns.Select(c => c.ValueGetter);

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
        AddOrReplaceColumn(column, obj => VariantValue.CreateFromObject(obj));
        return this;
    }

    /// <summary>
    /// Add data property that adds source object itself and convert it into JSON.
    /// </summary>
    /// <param name="description">Property description.</param>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(object)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize(object)")]
    public ClassRowsFrameBuilder<TClass> AddDataPropertyAsJson(string? description = null)
    {
        var column = new Column(DataColumn, DataType.String, description ?? "The raw JSON data representation.");
        _columns.Add((
            column,
            obj =>
            {
                try
                {
                    return VariantValue.CreateFromObject(JsonSerializer.Serialize(obj));
                }
                catch (JsonException)
                {
                    return VariantValue.Null;
                }
                catch (Exception)
                {
                    return VariantValue.Null;
                }
            }));
        AddOrReplaceColumn(column, obj => VariantValue.CreateFromObject(obj));
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
        var dataType = Converter.ConvertFromSystem(typeof(T));
        AddProperty(name, dataType, obj => VariantValue.CreateFromObject(valueGetter.Invoke(obj)), description,
            defaultLength);
        return this;
    }

    /// <summary>
    /// Add property.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="dataType">Property data type.</param>
    /// <param name="valueGetter">The delegate to get property value by object.</param>
    /// <param name="description">Property description.</param>
    /// <param name="defaultLength">Default column length.</param>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        DataType dataType,
        Func<TClass, VariantValue> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var column = new Column(name, dataType, description);
        if (defaultLength.HasValue)
        {
            column.Length = defaultLength.Value;
        }
        AddOrReplaceColumn(column, valueGetter.Invoke);
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
        var dataType = Converter.ConvertFromSystem(typeof(T));
        var valueGetter = valueGetterExpression.Compile();
        var column = new Column(propertyName, dataType, description);
        if (defaultLength.HasValue)
        {
            column.Length = defaultLength.Value;
        }
        AddOrReplaceColumn(column, obj => VariantValue.CreateFromObject(valueGetter.Invoke(obj)));
        return this;
    }

    #region AddProperty overloads

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, short?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(short?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, short> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(short));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, int?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(int?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, int> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(int));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, long?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(long?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, long> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(long));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, string?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(string));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)), description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, DateTime?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(DateTime?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, DateTime> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(DateTime));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, TimeSpan?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(TimeSpan?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, TimeSpan> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(TimeSpan));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, bool?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(bool?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, bool> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(bool));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, decimal?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(decimal?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, decimal> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(decimal));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, double?> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(double?));
        AddProperty(name, dataType, obj =>
            {
                var value = valueGetter.Invoke(obj);
                return value.HasValue ? new VariantValue(value.Value) : VariantValue.Null;
            }, description,
            defaultLength);
        return this;
    }

    public ClassRowsFrameBuilder<TClass> AddProperty(
        string name,
        Func<TClass, double> valueGetter,
        string? description = null,
        int? defaultLength = null)
    {
        var dataType = Converter.ConvertFromSystem(typeof(double));
        AddProperty(name, dataType, obj => new VariantValue(valueGetter.Invoke(obj)),
            description,
            defaultLength);
        return this;
    }

    #endregion

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

    private bool AddOrReplaceColumn(Column column, Func<TClass, VariantValue> valueGetter)
    {
        var existingColumnIndex = _columns.FindIndex(c => c.Column.Name == column.Name);
        if (existingColumnIndex > -1)
        {
            _columns[existingColumnIndex] = (column, valueGetter);
            return false;
        }
        else
        {
            _columns.Add((
                column,
                obj => VariantValue.CreateFromObject(valueGetter.Invoke(obj))
            ));
            return true;
        }
    }

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
