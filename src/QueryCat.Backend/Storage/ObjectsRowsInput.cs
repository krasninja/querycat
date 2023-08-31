using System.Reflection;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Creates rows input from POCO objects.
/// </summary>
/// <typeparam name="T">Object type.</typeparam>
public sealed class ObjectsRowsInput<T> : RowsInput
{
    private readonly IEnumerator<T> _enumerator;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    private sealed class PropertyInfoColumn : Column
    {
        public PropertyInfo PropertyInfo { get; }

        /// <inheritdoc />
        public PropertyInfoColumn(string name, DataType dataType, PropertyInfo propertyInfo, string? description = null)
            : base(name, dataType, description)
        {
            PropertyInfo = propertyInfo;
        }

        /// <inheritdoc />
        public PropertyInfoColumn(Column column, PropertyInfo propertyInfo) : base(column)
        {
            PropertyInfo = propertyInfo;
        }
    }

    public ObjectsRowsInput(IEnumerable<T> objects)
    {
        _enumerator = objects.GetEnumerator();
    }

    /// <inheritdoc />
    public override void Open()
    {
        var type = typeof(T);
        var columns = new List<Column>();
        foreach (var propertyInfo in type.GetProperties())
        {
            if (!propertyInfo.CanRead)
            {
                continue;
            }

            var dataType = Converter.ConvertFromSystem(propertyInfo.PropertyType);
            if (dataType == DataType.Void)
            {
                continue;
            }

            columns.Add(new PropertyInfoColumn(propertyInfo.Name, dataType, propertyInfo));
        }
        Columns = columns.ToArray();
    }

    /// <inheritdoc />
    public override void Close()
    {
        _enumerator.Dispose();
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var currentObject = _enumerator.Current;
        if (currentObject == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        var objValue = ((PropertyInfoColumn)Columns[columnIndex]).PropertyInfo.GetValue(currentObject);
        if (objValue == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }
        value = VariantValue.CreateFromObject(objValue);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();
        return _enumerator.MoveNext();
    }
}
