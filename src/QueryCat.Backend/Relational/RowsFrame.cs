using System.Collections;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Table is the in-memory store of rows. It is optimized for bulk rows store and
/// can be used for internal operations.
/// </summary>
public class RowsFrame : IRowsSchema, IEnumerable<Row>
{
    private readonly int _chunkSize;
    private readonly int _rowsPerChunk;
    private readonly ChunkList<VariantValue[]> _storage;
    private readonly Column[] _columns;

    /// <summary>
    /// Total rows.
    /// </summary>
    public int TotalRows { get; private set; }

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
        _chunkSize = options.ChunkSize;
        _columns = columns;
        if (_columns.Length < 1)
        {
            throw new ArgumentException(Resources.Errors.NoColumns, nameof(columns));
        }
        _rowsPerChunk = _chunkSize / _columns.Length;
        // Align.
        var remains = _chunkSize - _rowsPerChunk * _columns.Length;
        _chunkSize -= remains;

        _storage = new ChunkList<VariantValue[]>(_chunkSize);
    }

    public RowsFrame(params Column[] columns) : this(new RowsFrameOptions(), columns)
    {
    }

    /// <summary>
    /// Create rows frame from iterator.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator.</param>
    /// <returns>Instance of <see cref="RowsFrame" />.</returns>
    public static RowsFrame CreateFromIterator(IRowsIterator rowsIterator)
    {
        var rowsFrame = new RowsFrame(rowsIterator.Columns);
        while (rowsIterator.MoveNext())
        {
            rowsFrame.AddRow(rowsIterator.Current);
        }
        return rowsFrame;
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
        (int chunkIndex, int offset) = GetChunkAndOffset(rowIndex);
        if (chunkIndex > _storage.Count - 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidRowIndex, rowIndex));
        }
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
        (int chunkIndex, int offset) = GetChunkAndOffset(rowIndex);
        if (chunkIndex > _storage.Count - 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidRowIndex, rowIndex));
        }
        _storage[chunkIndex][offset + columnIndex] = value;
    }

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

    public IList<VariantValue> GetColumnValues(int columnIndex, int numberOfRows = -1)
    {
        var values = new List<VariantValue>(numberOfRows > 0 ? numberOfRows : TotalRows);
        for (int i = 0; i < TotalRows; i++)
        {
            (int chunkIndex, int offset) = GetChunkAndOffset(i);
            values.Add(_storage[chunkIndex][offset + columnIndex]);
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

    private (int ChunkIndex, int Offset) GetChunkAndOffset(int index)
    {
        var chunkIndex = index / _rowsPerChunk;
        return (chunkIndex, index % _rowsPerChunk * _columns.Length);
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
    public override string ToString() => $"Table (rows: {TotalRows})";

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
}
