using System.Text;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Represents a single query record.
/// </summary>
public class Row : IRowsSchema, ICloneable
{
    private readonly Column[] _columns;

    private readonly VariantValue[] _values;

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
        MakeEmpty();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Columns.</param>
    public Row(params Column[] columns)
    {
        _columns = columns;
        _values = new VariantValue[Columns.Length];
        MakeEmpty();
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
        var length = Math.Min(fromRow.Columns.Length, toRow.Columns.Length);
        for (int i = 0; i < length; i++)
        {
            toRow[i] = fromRow[i];
        }
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
        var length = Math.Min(fromRow.Columns.Length, toRow.Columns.Length);
        for (int i = fromRowOffset; i < length; i++)
        {
            toRow[toRowOffset + i] = fromRow[i];
        }
    }

    /// <summary>
    /// Return row as an array of values.
    /// </summary>
    /// <returns>Array of values.</returns>
    public VariantValue[] AsArray() => _values;

    /// <summary>
    /// Remove all the data, clear the row.
    /// </summary>
    public void MakeEmpty()
    {
        Array.Fill(_values, VariantValue.Null);
    }

    /// <summary>
    /// Index getter and setter.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    public VariantValue this[int columnIndex]
    {
        get => _values[columnIndex];
        set => _values[columnIndex] = value;
    }

    public VariantValue this[string columnName]
    {
        get
        {
            var columnIndex = this.GetColumnIndexByName(columnName);
            if (columnIndex < 0)
            {
                throw new ArgumentException($"Cannot find column '{columnName}'.", nameof(columnIndex));
            }
            return _values[columnIndex];
        }

        set
        {
            var columnIndex = this.GetColumnIndexByName(columnName);
            if (columnIndex < 0)
            {
                throw new ArgumentException($"Cannot find column '{columnName}'.", nameof(columnIndex));
            }
            _values[columnIndex] = value;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj is not Row row)
        {
            return false;
        }
        if (row._values.Length != _values.Length)
        {
            return false;
        }
        if (_columns != row._columns)
        {
            return false;
        }

        for (int i = 0; i < _values.Length; i++)
        {
            if (!row._values[i].Equals(_values[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Clear row values.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_values, VariantValue.Null);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        for (int i = 0; i < _values.Length; i++)
        {
            hashCode.Add(_values[i]);
        }
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(Columns.Length * 20);
        for (int i = 0; i < Columns.Length; i++)
        {
            sb.Append(_values[i].ToString());
            if (i != Columns.Length - 1)
            {
                sb.Append("; ");
            }
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public object Clone() => new Row(this);
}
