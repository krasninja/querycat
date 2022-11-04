using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

internal readonly struct CacheKeyCondition : IEquatable<CacheKeyCondition>
{
    public Column Column { get; }

    public VariantValue.Operation Operation { get; }

    /// <summary>
    /// First value.
    /// </summary>
    public VariantValue Value => ValuesArray.Values[0];

    public VariantValueArray ValuesArray { get; }

    public CacheKeyCondition(Column column, VariantValue.Operation operation, VariantValueArray valuesArray)
    {
        Column = column;
        Operation = operation;
        ValuesArray = valuesArray;
    }

    public CacheKeyCondition(Column column, VariantValue.Operation operation, params VariantValue[] values)
    {
        Column = column;
        Operation = operation;
        ValuesArray = new VariantValueArray(values);
    }

    #region Serialization

    internal string Serialize()
    {
        var values = ValuesArray.Values.Select(DataTypeUtils.SerializeVariantValue);
        return $"{Column.Name},{(int)Operation},{string.Join(",", values)}";
    }

    /// <summary>
    /// Deserialize from string. The string format is "columnName,operationIndex,value1,value2".
    /// </summary>
    /// <param name="columnFinder">Delegate to find column by name.</param>
    /// <param name="str">Target string.</param>
    /// <param name="condition">Out condition.</param>
    /// <returns><c>True</c> if created successfully, <c>false</c> otherwise.</returns>
    internal static bool Deserialize(Func<string, Column?> columnFinder, string str, out CacheKeyCondition condition)
    {
        var arr = StringUtils.GetFieldsFromLine(str);
        if (arr.Length < 3)
        {
            condition = default;
            return false;
        }

        var column = columnFinder.Invoke(arr[0]);
        if (column == null)
        {
            condition = default;
            return false;
        }

        var values = arr[2..].Select(v => DataTypeUtils.DeserializeVariantValue(v));
        condition = new CacheKeyCondition(
            column,
            (VariantValue.Operation)int.Parse(arr[1]),
            new VariantValueArray(values.ToArray()));
        return true;
    }

    #endregion

    #region Equals

    /// <inheritdoc />
    public bool Equals(CacheKeyCondition other)
        => Column.Equals(other.Column) && Operation == other.Operation && ValuesArray.Equals(other.ValuesArray);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is CacheKeyCondition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Column, (int)Operation, ValuesArray);

    public static bool operator ==(CacheKeyCondition left, CacheKeyCondition right) => left.Equals(right);

    public static bool operator !=(CacheKeyCondition left, CacheKeyCondition right) => !left.Equals(right);

    #endregion

    /// <inheritdoc />
    public override string ToString() => Serialize();
}
