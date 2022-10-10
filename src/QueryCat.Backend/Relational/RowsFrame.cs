using System.Collections;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

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

    public int TotalRows { get; private set; }

    public bool IsEmpty => TotalRows == 0;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="columns">Columns.</param>
    public RowsFrame(RowsFrameOptions options, params Column[] columns)
    {
        _chunkSize = options.ChunkSize;
        if (_chunkSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.ChunkSize));
        }
        _columns = columns;
        if (_columns.Length < 1)
        {
            throw new ArgumentException("There are no columns.", nameof(columns));
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
    /// Add row into rows frame.
    /// </summary>
    /// <param name="row">Row to add.</param>
    /// <returns>Inserted row index.</returns>
    public int AddRow(Row row)
    {
        (int chunkIndex, int offset) = EnsureCapacityAndGetStartOffset(TotalRows);
        Array.Copy(row.AsArray(), 0, _storage[chunkIndex], offset, _columns.Length);
        return TotalRows++;
    }

    public int AddRow(params object[] items)
    {
        (int chunkIndex, int offset) = EnsureCapacityAndGetStartOffset(TotalRows);
        for (int i = 0; i < _columns.Length; i++)
        {
            if (items[i] is VariantValue rowValue)
            {
                _storage[chunkIndex][offset + i] = rowValue;
            }
            else
            {
                _storage[chunkIndex][offset + i] = VariantValue.CreateFromObject(items[i]);
            }
        }
        return TotalRows++;
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

    public void Clear()
    {
        _storage.Clear();
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
