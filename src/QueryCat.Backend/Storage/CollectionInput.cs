using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class that allow to represent enumerable as rows input/output.
/// </summary>
/// <typeparam name="TClass">Enumerable item type.</typeparam>
public class CollectionInput<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TClass>
    : IRowsOutput, IDisposable, IRowsInputUpdate where TClass : class, new()
{
    private readonly IEnumerable<TClass> _list;
    private readonly List<PropertyInfo> _columnsProperties = new();
    private readonly IEnumerator<TClass> _enumerator;
    private Column[] _columns = new Column[0];
    private PropertyInfo[]? _propertiesMapping;

    public IEnumerable<TClass> TargetCollection => _list;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public string[] UniqueKey { get; } = Array.Empty<string>();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public RowsOutputOptions Options { get; } = new();

    public CollectionInput(IEnumerable<TClass> list)
    {
        _list = list;
        _enumerator = _list.GetEnumerator();

        FillColumns();
    }

    private void FillColumns()
    {
        var builder = new ClassRowsFrameBuilder<TClass>();
        builder.AddPublicProperties(out var properties);
        _columnsProperties.AddRange(properties);
        Array.Resize(ref _columns, builder.Columns.Count());
        builder.Columns.ToArray().CopyTo(_columns, 0);
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void Close()
    {
        _enumerator.Dispose();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _enumerator.Reset();
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var obj = _enumerator.Current;
        value = VariantValue.CreateFromObject(_columnsProperties[columnIndex].GetValue(obj));
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public void WriteValues(in VariantValue[] values)
    {
        if (_list is not ICollection<TClass> collection)
        {
            throw new QueryCatException($"Cannot write to collection of type '{_list.GetType().Name}'.");
        }

        var columns = QueryContext.QueryInfo.Columns.ToArray();
        var mapping = GetPropertiesToColumnsMapping(columns);

        var obj = new TClass();
        for (var i = 0; i < mapping.Length; i++)
        {
            var prop = _columnsProperties[i];
            if (!prop.CanWrite)
            {
                continue;
            }
            prop.SetValue(obj, ChangeType(values[i].GetGenericObject(), prop.PropertyType));
        }
        collection.Add(obj);
    }

    private PropertyInfo[] GetPropertiesToColumnsMapping(Column[] columns)
    {
        if (_propertiesMapping != null)
        {
            return _propertiesMapping;
        }
        var mapList = new List<PropertyInfo>();
        foreach (var column in columns)
        {
            var columnIndex = this.GetColumnIndexByName(column.Name, column.SourceName);
            if (columnIndex < 0)
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotFindColumn, column.FullName));
            }
            mapList.Add(_columnsProperties[columnIndex]);
        }
        _propertiesMapping = mapList.ToArray();
        return _propertiesMapping;
    }

    /// <inheritdoc />
    public bool ReadNext() => _enumerator.MoveNext();

    /// <inheritdoc />
    public ErrorCode UpdateValue(int columnIndex, in VariantValue value)
    {
        var obj = _enumerator.Current;
        var prop = _columnsProperties[columnIndex];
        if (!prop.CanWrite)
        {
            throw new QueryCatException($"Cannot write property '{prop.Name}'.");
        }
        prop.SetValue(obj, ChangeType(value.GetGenericObject(), prop.PropertyType));
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Collection (type={typeof(TClass).Name})");
    }

    /// <summary>
    /// Change object type. The method takes into account also nullable types.
    /// </summary>
    /// <param name="value">Object to type change.</param>
    /// <param name="conversionType">Conversion type.</param>
    /// <returns>New object with target type.</returns>
    internal static object? ChangeType(object? value, Type conversionType)
    {
        if (value == null)
        {
            return null;
        }

        if (Nullable.GetUnderlyingType(conversionType) != null)
        {
            conversionType = Nullable.GetUnderlyingType(conversionType) ?? conversionType;
        }
        return Convert.ChangeType(value, conversionType);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
