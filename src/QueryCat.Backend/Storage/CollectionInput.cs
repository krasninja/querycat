using System.Collections;
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
public class CollectionInput : IRowsOutput, IDisposable, IAsyncDisposable, IRowsInputUpdate
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _type;
    private readonly bool _isSimple;
    private readonly IEnumerable _enumerable;
    private readonly List<PropertyInfo> _columnsProperties = new();
    private readonly IEnumerator _enumerator;
    private Column[] _columns = [];
    private PropertyInfo[]? _propertiesMapping;

    public virtual IEnumerable TargetCollection => _enumerable;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public string[] UniqueKey { get; } = [];

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public RowsOutputOptions Options { get; } = new();

    public CollectionInput(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                                    | DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        IEnumerable enumerable)
    {
        _type = type;
        _isSimple = IsSimpleType(_type);
        _enumerable = enumerable;
        // ReSharper disable once NotDisposedResource
        _enumerator = _enumerable.GetEnumerator();

        FillColumns();
    }

    private void FillColumns()
    {
        var builder = new ClassRowsFrameBuilder<object>();
        if (!_isSimple)
        {
            builder.AddPublicProperties(_type, out var properties);
            _columnsProperties.AddRange(properties);
            Array.Resize(ref _columns, builder.Columns.Count);
            builder.Columns.ToArray().CopyTo(_columns, 0);
        }
        else
        {
            _columns =
            [
                new Column(
                    name: "value",
                    sourceName: string.Empty,
                    dataType: Converter.ConvertFromSystem(_type)
                )
            ];
        }
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _enumerator.Reset();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var obj = _enumerator.Current;
        if (!_isSimple)
        {
            value = VariantValue.CreateFromObject(_columnsProperties[columnIndex].GetValue(obj));
        }
        else
        {
            value = VariantValue.CreateFromObject(obj);
        }
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_enumerator.MoveNext());

    /// <inheritdoc />
    public ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        if (_enumerable is not IList list)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CannotWriteToCollection, _enumerable.GetType().Name));
        }

        var columns = QueryContext.QueryInfo.Columns.ToArray();
        var mapping = GetPropertiesToColumnsMapping(columns);

        var obj = Activator.CreateInstance(_type);
        for (var i = 0; i < mapping.Length; i++)
        {
            var prop = _columnsProperties[i];
            if (!prop.CanWrite)
            {
                continue;
            }
            prop.SetValue(obj, ChangeType(values[i].GetGenericObject(), prop.PropertyType));
        }
        list.Add(obj);

        return ValueTask.FromResult(ErrorCode.OK);
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
    public ValueTask<ErrorCode> UpdateValueAsync(int columnIndex, VariantValue value, CancellationToken cancellationToken = default)
    {
        var obj = _enumerator.Current;
        var prop = _columnsProperties[columnIndex];
        if (!prop.CanWrite)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CannotWriteToProperty, prop.Name));
        }
        prop.SetValue(obj, ChangeType(value.GetGenericObject(), prop.PropertyType));
        return ValueTask.FromResult(ErrorCode.OK);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Collection (type={_type.Name})");
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

    private static readonly Type[] _simpleTypes =
    {
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
    };

    private static bool IsSimpleType(in Type type)
    {
        return type.IsPrimitive
               || type == typeof(string)
               || Array.IndexOf(_simpleTypes, type) > -1
               || type.IsEnum
               || (type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                   && IsSimpleType(type.GetGenericArguments()[0]));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_enumerator as IDisposable)?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_enumerable is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            (_enumerator as IDisposable)?.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
