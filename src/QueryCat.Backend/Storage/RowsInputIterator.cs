using System.Runtime.CompilerServices;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Iterator for <see cref="IRowsInput" />.
/// </summary>
public class RowsInputIterator : IRowsIterator
{
    private readonly IRowsInput _rowsInput;
    private readonly bool _autoFetch;
    private Row _row;
    private bool[] _fetchedColumnsIndexes = Array.Empty<bool>();
    private bool _hasInput;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public Row Current => _row;

    public RowsInputIterator(IRowsInput rowsInput, bool autoFetch = true)
    {
        _rowsInput = rowsInput;
        _autoFetch = autoFetch;
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
    /// <param name="columnsIndex">Column index.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FetchValueForColumn(int columnsIndex)
    {
        // Postpone columns and row initialization because some row inputs
        // has columns initialized only after first MoveNext() call.
        if (_fetchedColumnsIndexes.Length == 0)
        {
            _fetchedColumnsIndexes = new bool[Columns.Length];
            _row = new Row(this);
        }
        if (!_hasInput || _fetchedColumnsIndexes[columnsIndex])
        {
            return;
        }
        _rowsInput.ReadValue(columnsIndex, out VariantValue variantValue);
        _row[columnsIndex] = variantValue;
        if (!_autoFetch)
        {
            _fetchedColumnsIndexes[columnsIndex] = true;
        }
    }

    public void FetchValuesForColumns(params int[] columnsIndexes)
    {
        for (int i = 0; i < columnsIndexes.Length; i++)
        {
            var index = columnsIndexes[i];
            FetchValueForColumn(index);
        }
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        _hasInput = _rowsInput.ReadNext();
        if (!_hasInput)
        {
            Logger.Instance.Debug($"Close rows input {_rowsInput}.", nameof(RowsInputIterator));
            _rowsInput.Close();
        }
        if (_hasInput && _autoFetch)
        {
            FetchValuesForAllColumns();
        }
        if (!_autoFetch)
        {
            Array.Fill(_fetchedColumnsIndexes, false);
        }
        return _hasInput;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _row.Clear();
        _rowsInput.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Input {_rowsInput.GetType().Name} (autofetch={_autoFetch})");
        stringBuilder.IncreaseIndent();
        var rowsInputString = _rowsInput.ToString();
        if (!string.IsNullOrEmpty(rowsInputString))
        {
            stringBuilder.AppendLine(rowsInputString);
        }
        stringBuilder.DecreaseIndent();
    }
}
