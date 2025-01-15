using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Builder simplify the process of objects mapping to rows frame.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
[DebuggerDisplay("Columns = {Columns.Count}")]
public class ClassRowsFrameBuilder<TClass> where TClass : class
{
    private const string DataColumn = "__data";

    private readonly record struct ColumnGetter(Column Column, Func<TClass, VariantValue> ValueGetter);

    private readonly List<ColumnGetter> _columns = new();
    private readonly List<KeyColumn> _keyColumns = new();

    /// <summary>
    /// Property naming convention when used from expressions.
    /// </summary>
    public NamingConventionStyle NamingConvention { get; set; } = NamingConventionStyle.Keep;

    /// <summary>
    /// Columns.
    /// </summary>
    public IReadOnlyList<Column> Columns => _columns.Select(c => c.Column).ToList();

    /// <summary>
    /// Key columns.
    /// </summary>
    public IReadOnlyList<KeyColumn> KeyColumns => _keyColumns;

    /// <summary>
    /// Columns.
    /// </summary>
    public IReadOnlyList<Func<TClass, VariantValue>> Getters => _columns.Select(c => c.ValueGetter).ToList();

    /// <summary>
    /// Add data property that adds source object itself.
    /// </summary>
    /// <param name="description">Property description.</param>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddDataProperty(string? description = null)
    {
        var column = new Column(DataColumn, DataType.Object, description ?? "The raw data representation.");
        AddOrReplaceColumn(
            column,
            obj => VariantValue.CreateFromObject(obj)
        );
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
        AddOrReplaceColumn(
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
            });
        return this;
    }

    /// <summary>
    /// Add data property that adds source object itself as JSON.
    /// </summary>
    /// <param name="valueGetter">The delegate to get property JSON value by object.</param>
    /// <param name="description">Property description.</param>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddDataPropertyAsJson(
        Func<TClass, JsonNode> valueGetter,
        string? description = null)
    {
        var column = new Column(DataColumn, DataType.String, description ?? "The raw JSON data representation.");
        AddOrReplaceColumn(
            column,
            obj =>
            {
                try
                {
                    return VariantValue.CreateFromObject(valueGetter.Invoke(obj));
                }
                catch (JsonException)
                {
                    return VariantValue.Null;
                }
            });
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
            throw new InvalidOperationException(Resources.Errors.CannotGetColumnFromExpression);
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

    internal ClassRowsFrameBuilder<TClass> AddProperty(
        PropertyInfo propertyInfo)
    {
        var description = propertyInfo.GetCustomAttributes<DescriptionAttribute>()
            .Select(a => a.Description).FirstOrDefault();
        var dataType = Converter.ConvertFromSystem(propertyInfo.PropertyType);
        AddProperty(
            name: propertyInfo.Name,
            dataType: dataType,
            obj => VariantValue.CreateFromObject(propertyInfo.GetValue(obj)),
            description: description);
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
            _columns[existingColumnIndex] = new ColumnGetter(column, valueGetter);
            return false;
        }
        else
        {
            _columns.Add(new ColumnGetter(
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

    #region Keys

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <returns>Instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddKeyColumn(
        string columnName,
        bool isRequired = false)
    {
        var columnIndex = GetColumnIndexByName(columnName);
        var keyColumn = new KeyColumn(columnIndex, isRequired);
        _keyColumns.Add(keyColumn);
        return this;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <returns>Instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        bool isRequired = false)
    {
        var columnIndex = GetColumnIndexByName(columnName);
        var keyColumn = new KeyColumn(columnIndex, isRequired, operation);
        _keyColumns.Add(keyColumn);
        return this;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="orOperation">Alternate key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <returns>Instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public ClassRowsFrameBuilder<TClass> AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        VariantValue.Operation orOperation,
        bool isRequired = false)
    {
        var columnIndex = GetColumnIndexByName(columnName);
        var keyColumn = new KeyColumn(columnIndex, isRequired, operation, orOperation);
        _keyColumns.Add(keyColumn);
        return this;
    }

    #endregion

    private int GetColumnIndexByName(string columnName)
    {
        var columnIndex = _columns.FindIndex(c => Column.NameEquals(c.Column, columnName));
        if (columnIndex < 0)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CannotFindColumn, columnName));
        }
        return columnIndex;
    }
}
