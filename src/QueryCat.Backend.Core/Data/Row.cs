using System.Collections;
using System.Globalization;
using System.Text;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Represents a single query record.
/// </summary>
public class Row : IRowsSchema, ICloneable, IEnumerable<VariantValue>
{
    /// <summary>
    /// Empty instance with no columns.
    /// </summary>
    internal static Row Empty { get; } = new();

    private readonly Column[] _columns;

    private readonly VariantValue[] _values;

    /// <summary>
    /// Get value by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    public VariantValue this[string columnName]
    {
        get
        {
            var columnIndex = this.GetColumnIndexByName(columnName);
            if (columnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(string.Format(Resources.Errors.CannotFindColumn, columnName), nameof(columnIndex));
            }
            return _values[columnIndex];
        }

        set
        {
            var columnIndex = this.GetColumnIndexByName(columnName);
            if (columnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(string.Format(Resources.Errors.CannotFindColumn, columnName), nameof(columnIndex));
            }
            _values[columnIndex] = value;
        }
    }

    /// <summary>
    /// Index getter and setter.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    public virtual VariantValue this[int columnIndex]
    {
        get => _values[columnIndex];
        set => _values[columnIndex] = value;
    }

    /// <summary>
    /// Row values array.
    /// </summary>
    protected internal VariantValue[] Values => _values;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="schema">Related rows schema.</param>
    public Row(IRowsSchema schema)
    {
        _columns = schema.Columns;
        _values = new VariantValue[Columns.Length];
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Columns.</param>
    public Row(params Column[] columns)
    {
        _columns = columns;
        _values = new VariantValue[Columns.Length];
    }

    /// <summary>
    /// Constructor to create the new row and copy values from the existing one.
    /// </summary>
    /// <param name="row">The row to copy structure and data.</param>
    public Row(Row row)
    {
        _values = new VariantValue[row._values.Length];
        Array.Copy(row._values, _values, row._values.Length);
        _columns = row._columns;
    }

    /// <summary>
    /// Copy row values into another row.
    /// </summary>
    /// <param name="fromRow">Source row.</param>
    /// <param name="toRow">Destination row.</param>
    public static void Copy(Row fromRow, Row toRow)
    {
        ValidateArraysLength(fromRow._values, toRow._values);
        fromRow._values.CopyTo(toRow._values, 0);
    }

    /// <summary>
    /// Copy row values into another row.
    /// </summary>
    /// <param name="values">Values.</param>
    /// <param name="toRow">Destination row.</param>
    public static void Copy(ReadOnlySpan<VariantValue> values, Row toRow)
    {
        ValidateArraysLength(values, toRow._values);
        values.CopyTo(toRow._values);
    }

    /// <summary>
    /// Copy row values into another row starting from index.
    /// </summary>
    /// <param name="fromRow">Source row.</param>
    /// <param name="fromRowOffset">Source start index.</param>
    /// <param name="toRow">Destination row.</param>
    /// <param name="toRowOffset">Destination start index.</param>
    public static void Copy(Row fromRow, int fromRowOffset, Row toRow, int toRowOffset)
    {
        fromRow._values[fromRowOffset..].CopyTo(toRow._values.AsSpan(toRowOffset));
    }

    private static void ValidateArraysLength(ReadOnlySpan<VariantValue> array1, ReadOnlySpan<VariantValue> array2)
    {
        if (array1.Length > array2.Length)
        {
            throw new ArgumentException(Resources.Errors.InvalidNumberOfValues);
        }
    }

    /// <summary>
    /// Return row as an array of values.
    /// </summary>
    /// <param name="copy">Return the copy of row values, otherwise it returns internal array.</param>
    /// <returns>Array of values.</returns>
    public VariantValue[] AsArray(bool copy = false)
    {
        if (!copy)
        {
            return _values;
        }
        else
        {
            var arr = new VariantValue[_values.Length];
            Array.Copy(_values, arr, arr.Length);
            return arr;
        }
    }

    /// <summary>
    /// Clear row values.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_values, VariantValue.Null);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Row row)
        {
            return false;
        }
        if (row._values.Length != _values.Length)
        {
            return false;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (!row._values[i].Equals(_values[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        foreach (var value in _values)
        {
            hashCode.Add(value);
        }
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(Columns.Length * 20);
        for (var i = 0; i < Columns.Length; i++)
        {
            sb.Append(_values[i].ToString(CultureInfo.InvariantCulture));
            if (i != Columns.Length - 1)
            {
                sb.Append("; ");
            }
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator()
    {
        foreach (var value in _values)
        {
            yield return value;
        }
    }

    /// <inheritdoc />
    public object Clone() => new Row(this);
}
