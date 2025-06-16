using System.Collections;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Table is the in-memory store of rows. It is optimized for bulk rows store and
/// can be used for internal operations. The remove operation doesn't physically remove
/// it from memory and just marks the row. Use IsRemoved method to check by the row index.
/// </summary>
public class RowsFrame : IRowsSchema, IEnumerable<Row>
{
    private readonly int _chunkSize;
    private readonly int _rowsPerChunk;
    private readonly ChunkList<VariantValue[]> _storage;
    private readonly Column[] _columns;
    private readonly HashSet<int> _removedRows = new();

    /// <summary>
    /// Total rows.
    /// </summary>
    public int TotalRows { get; private set; }

    /// <summary>
    /// Total active rows.
    /// </summary>
    public int TotalActiveRows => TotalRows - _removedRows.Count;

    /// <summary>
    /// Is the rows frame empty.
    /// </summary>
    public bool IsEmpty => TotalRows == 0;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <summary>
    /// Empty instance.
    /// </summary>
    public static RowsFrame Empty { get; } = new(new Column("__empty", DataType.Integer));

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="columns">Columns.</param>
    public RowsFrame(RowsFrameOptions options, params Column[] columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ChunkSize, nameof(options.ChunkSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(columns.Length, 1, nameof(columns));
        _chunkSize = options.ChunkSize;
        _columns = columns;
        _rowsPerChunk = _chunkSize / _columns.Length;
        // Align.
        var remains = _chunkSize - _rowsPerChunk * _columns.Length;
        _chunkSize -= remains;

        _storage = new ChunkList<VariantValue[]>(_chunkSize);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Frame columns.</param>
    public RowsFrame(params Column[] columns) : this(new RowsFrameOptions(), columns)
    {
    }

    /// <summary>
    /// Returns the value of first row and first column or null.
    /// </summary>
    /// <param name="rowIndex">Row index, zero by default.</param>
    /// <returns>The first value or null.</returns>
    public VariantValue GetFirstValue(int rowIndex = 0)
    {
        (int chunkIndex, int offset) = EnsureCapacityAndGetStartOffset(rowIndex);
        return _storage[chunkIndex][offset];
    }

    /// <summary>
    /// Get value at specific row and column.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <returns>Value.</returns>
    public VariantValue GetValue(int rowIndex = 0, int columnIndex = 0)
    {
        (int chunkIndex, int offset) = GetChunkAndOffset(rowIndex);
        return _storage[chunkIndex][offset + columnIndex];
    }

    /// <summary>
    /// Add row into rows frame.
    /// </summary>
    /// <param name="row">Row to add.</param>
    /// <returns>Inserted row index.</returns>
    public int AddRow(Row row) => AddRow(row.Values);

    /// <summary>
    /// Add values into rows frame.
    /// </summary>
    /// <param name="values">Values to add.</param>
    /// <returns>Inserted row index.</returns>
    public int AddRow(VariantValue[] values)
    {
        if (values.Length != Columns.Length)
        {
            throw new QueryCatException(Resources.Errors.ColumnsCountNoMatch);
        }
        (int chunkIndex, int offset) = EnsureCapacityAndGetStartOffset(TotalRows);
        Array.Copy(values, 0, _storage[chunkIndex], offset, _columns.Length);
        return TotalRows++;
    }

    /// <summary>
    /// Add row.
    /// </summary>
    /// <param name="values">Values to add.</param>
    public void AddRow(params object[] values)
    {
        (int chunkIndex, int offset) = EnsureCapacityAndGetStartOffset(TotalRows);
        for (int i = 0; i < _columns.Length; i++)
        {
            if (values[i] is VariantValue rowValue)
            {
                _storage[chunkIndex][offset + i] = rowValue;
            }
            else
            {
                _storage[chunkIndex][offset + i] = VariantValue.CreateFromObject(values[i]);
            }
        }
        TotalRows++;
    }

    /// <summary>
    /// Read row by specific index.
    /// </summary>
    /// <param name="row">Row to read data to.</param>
    /// <param name="rowIndex">Row index.</param>
    public void ReadRowAt(Row row, int rowIndex)
    {
        var (chunkIndex, offset) = GetChunkAndOffsetValidate(rowIndex);
        Array.Copy(_storage[chunkIndex], offset, row.AsArray(), 0, _columns.Length);
    }

    /// <summary>
    /// Update value at specific position.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">New value.</param>
    public void UpdateValue(int rowIndex, int columnIndex, VariantValue value)
    {
        var (chunkIndex, offset) = GetChunkAndOffsetValidate(rowIndex);
        _storage[chunkIndex][offset + columnIndex] = value;
    }

    /// <summary>
    /// Update range of values.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndexOffset">Zero-based columns offset.</param>
    /// <param name="values">Values to copy.</param>
    public void UpdateValues(int rowIndex, int columnIndexOffset, params ReadOnlySpan<VariantValue> values)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length + columnIndexOffset,
            Columns.Length, nameof(values));

        var (chunkIndex, offset) = GetChunkAndOffsetValidate(rowIndex);
        values.CopyTo(_storage[chunkIndex].AsSpan(offset + columnIndexOffset, values.Length));
    }

    /// <summary>
    /// Mark the row as removed.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    public bool RemoveRow(int rowIndex)
    {
        (int chunkIndex, int offset) = GetChunkAndOffset(rowIndex);
        if (chunkIndex > _storage.Count - 1)
        {
            return false;
        }
        if (_removedRows.Add(rowIndex))
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                _storage[chunkIndex][offset] = VariantValue.Null;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns <c>true</c> if row is marked as removed.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <returns>Returns <c>true</c> if row is marked as removed, <c>false</c> otherwise.</returns>
    public bool IsRemoved(int rowIndex) => _removedRows.Contains(rowIndex);

    /// <summary>
    /// Get row by index.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <returns>Instance of <see cref="Row" />.</returns>
    public Row GetRow(int rowIndex)
    {
        var row = new Row(this);
        ReadRowAt(row, rowIndex);
        return row;
    }

    /// <summary>
    /// Get all values of the specified column.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="numberOfRows">Max number of rows to retrieve. -1 to get all.</param>
    /// <returns>List of values.</returns>
    public IList<VariantValue> GetColumnValues(int columnIndex, int numberOfRows = -1)
    {
        var max = numberOfRows > 0 ? numberOfRows : TotalRows;
        var values = new List<VariantValue>(max);
        foreach (var item in this)
        {
            values.Add(item[columnIndex]);
        }
        return values;
    }

    /// <summary>
    /// Clear storage.
    /// </summary>
    public void Clear()
    {
        _storage.Clear();
        TotalRows = 0;
    }

    private (int ChunkIndex, int Offset) GetChunkAndOffsetValidate(int rowIndex)
    {
        var chunkIndex = rowIndex / _rowsPerChunk;
        if (chunkIndex > _storage.Count - 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidRowIndex, rowIndex));
        }
        return (chunkIndex, rowIndex % _rowsPerChunk * _columns.Length);
    }

    private (int ChunkIndex, int Offset) GetChunkAndOffset(int rowIndex)
    {
        var chunkIndex = rowIndex / _rowsPerChunk;
        return (chunkIndex, rowIndex % _rowsPerChunk * _columns.Length);
    }

    private (int ChunkIndex, int Offset) EnsureCapacityAndGetStartOffset(int index)
    {
        var pos = GetChunkAndOffset(index);
        while (pos.ChunkIndex > _storage.Count - 1)
        {
            _storage.Add(new VariantValue[_chunkSize]);
        }
        return pos;
    }

    /// <inheritdoc />
    public IEnumerator<Row> GetEnumerator()
    {
        var iterator = new RowsFrameIterator(this);
        while (iterator.MoveNext())
        {
            yield return new Row(iterator.Current);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates new rows iterator.
    /// </summary>
    /// <param name="childIterator">Child iterator.</param>
    /// <returns>Instance of <see cref="IRowsIterator" />.</returns>
    public RowsFrameIterator GetIterator(IRowsIterator? childIterator = null) => new(this);

    /// <inheritdoc />
    public override string ToString() => $"Table (rows: {TotalRows})";
}
