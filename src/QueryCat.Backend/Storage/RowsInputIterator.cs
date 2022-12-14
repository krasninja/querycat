using System.Runtime.CompilerServices;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Iterator for <see cref="IRowsInput" />.
/// </summary>
public class RowsInputIterator : IRowsIterator, IDisposable
{
    private readonly IRowsInput _rowsInput;
    private readonly bool _autoOpen;
    private bool _isOpened;
    private Row _row;
    private bool[] _fetchedColumnsIndexes = Array.Empty<bool>();
    private bool _hasInput;
    private int _rowIndex;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public Row Current => _row;

    /// <summary>
    /// Autofetch all values.
    /// </summary>
    public bool AutoFetch { get; set; }

    /// <summary>
    /// The event occurs on data processing (reading) errors.
    /// </summary>
    public event EventHandler<RowsInputErrorEventArgs>? OnError;

    public IRowsInput RowsInput => _rowsInput;

    public RowsInputIterator(IRowsInput rowsInput, bool autoFetch = true, bool autoOpen = false)
    {
        _rowsInput = rowsInput;
        AutoFetch = autoFetch;
        _autoOpen = autoOpen;
        _row = new Row(this);
    }

    /// <summary>
    /// Read values for all columns.
    /// </summary>
    public void FetchValuesForAllColumns()
    {
        for (var i = 0; i < Columns.Length; i++)
        {
            FetchValueForColumn(i);
        }
    }

    /// <summary>
    /// Read value for the specific column from rows input.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FetchValueForColumn(int columnIndex)
    {
        // Postpone columns and row initialization because some row inputs
        // has columns initialized only after first MoveNext() call.
        if (_fetchedColumnsIndexes.Length == 0)
        {
            _fetchedColumnsIndexes = new bool[Columns.Length];
            _row = new Row(this);
        }
        if (!_hasInput || _fetchedColumnsIndexes[columnIndex])
        {
            return;
        }
        var errorCode = _rowsInput.ReadValue(columnIndex, out var variantValue);
        if (errorCode != ErrorCode.OK)
        {
            OnError?.Invoke(this, new RowsInputErrorEventArgs(_rowIndex, columnIndex, errorCode));
        }
        _row[columnIndex] = variantValue;
        if (!AutoFetch)
        {
            _fetchedColumnsIndexes[columnIndex] = true;
        }
    }

    public void FetchValuesForColumns(params int[] columnsIndexes)
    {
        for (var i = 0; i < columnsIndexes.Length; i++)
        {
            var index = columnsIndexes[i];
            FetchValueForColumn(index);
        }
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_autoOpen && !_isOpened)
        {
            _rowsInput.Open();
            _isOpened = true;
        }

        _hasInput = _rowsInput.ReadNext();
        if (_hasInput && AutoFetch)
        {
            FetchValuesForAllColumns();
        }
        if (!AutoFetch)
        {
            Array.Fill(_fetchedColumnsIndexes, false);
        }
        _rowIndex++;
        return _hasInput;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _row.Clear();
        _rowsInput.Reset();
        _rowIndex = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Input {_rowsInput.GetType().Name} (autofetch={AutoFetch})");
        stringBuilder.IncreaseIndent();
        _rowsInput.Explain(stringBuilder);
        stringBuilder.DecreaseIndent();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rowsInput.Close();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
