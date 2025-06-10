using System.Runtime.CompilerServices;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Iterator for <see cref="IRowsInput" />.
/// </summary>
public class RowsInputIterator : IRowsIterator, IRowsIteratorParent
{
    private sealed class CacheInputRow : Row
    {
        private readonly RowsInputIterator _rowsInputIterator;
        private readonly bool[] _fetched;

        public CacheInputRow(RowsInputIterator rowsInputIterator) : base(rowsInputIterator)
        {
            _rowsInputIterator = rowsInputIterator;
            _fetched = new bool[rowsInputIterator.Columns.Length];
        }

        /// <inheritdoc />
        public override VariantValue this[int columnIndex]
        {
            get
            {
                if (!_fetched[columnIndex])
                {
                    FetchValue(columnIndex);
                    _fetched[columnIndex] = true;
                }
                return base[columnIndex];
            }

            set
            {
                _fetched[columnIndex] = true;
                base[columnIndex] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void FetchValue(int columnIndex)
        {
            var errorCode = _rowsInputIterator._rowsInput.ReadValue(columnIndex, out var value);
            _rowsInputIterator.AddError(errorCode, columnIndex + 1);
            Values[columnIndex] = value;
        }

        public void Reset() => Array.Fill(_fetched, false);
    }

    private readonly int _id = IdGenerator.GetNext();

    private readonly IRowsInput _rowsInput;
    private readonly bool _autoOpen;
    private bool _isOpened;
    private Row _row;
    private bool _hasInput;
    private int _rowIndex;
    private bool _isInitialized;
    private bool _isFirstRowPrefetched;

    private readonly ExecutionStatistic? _statistic;
    private readonly bool _useDetailedStatistic;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public Row Current => _row;

    /// <summary>
    /// Autofetch all values.
    /// </summary>
    public bool AutoFetch { get; set; }

    /// <summary>
    /// Related rows input.
    /// </summary>
    public IRowsInput RowsInput => _rowsInput;

    public RowsInputIterator(
        IRowsInput rowsInput,
        bool autoFetch = true,
        bool autoOpen = false,
        ExecutionStatistic? statistic = null,
        bool detailedStatistic = false)
    {
        _rowsInput = rowsInput;
        AutoFetch = autoFetch;
        _autoOpen = autoOpen;
        _row = new Row(this);
        _statistic = statistic;
        _useDetailedStatistic = detailedStatistic;
    }

    private void AddError(ErrorCode errorCode, int columnIndex)
    {
        if (_statistic == null || errorCode == ErrorCode.OK)
        {
            return;
        }

        if (_useDetailedStatistic)
        {
            _statistic.AddError(
                new ExecutionStatistic.RowErrorInfo(errorCode, _rowIndex, columnIndex));
        }
        else
        {
            _statistic.AddError(new ExecutionStatistic.RowErrorInfo(errorCode));
        }
    }

    private void FetchAll()
    {
        for (var i = 0; i < Columns.Length; i++)
        {
            var errorCode = _rowsInput.ReadValue(i, out var value);
            AddError(errorCode, i + 1);
            _row[i] = value;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (_isFirstRowPrefetched)
        {
            _isFirstRowPrefetched = false;
            return true;
        }

        // Open rows input.
        if (_autoOpen && !_isOpened)
        {
            await _rowsInput.OpenAsync(cancellationToken);
            _isOpened = true;
        }

        // Read.
        _hasInput = await _rowsInput.ReadNextAsync(cancellationToken);

        // Postpone columns and row initialization because some row inputs
        // has columns initialized only after first MoveNext() call.
        if (!_isInitialized)
        {
            _row = AutoFetch ? new Row(this) : new CacheInputRow(this);
            _isInitialized = true;
        }

        // Reset row.
        if (_row is CacheInputRow cacheInputRow)
        {
            cacheInputRow.Reset();
        }
        else
        {
            _row.Clear();
        }

        // Fetch.
        if (_hasInput && AutoFetch)
        {
            FetchAll();
        }

        _rowIndex++;
        return _hasInput;
    }

    public async ValueTask PrefetchFirstRowAsync(CancellationToken cancellationToken = default)
    {
        var hasData = await this.MoveNextAsync(cancellationToken);
        if (hasData)
        {
            _isFirstRowPrefetched = true;
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsInput.ResetAsync(cancellationToken);
        if (!_isInitialized)
        {
            return;
        }
        _row.Clear();
        _rowIndex = 0;
        _isInitialized = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Input {_rowsInput.GetType().Name} (autofetch={AutoFetch}, id={_id})");
        stringBuilder.IncreaseIndent();
        _rowsInput.Explain(stringBuilder);
        stringBuilder.DecreaseIndent();
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsInput;
    }

    /// <summary>
    /// Close the inner rows input.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _rowsInput.CloseAsync(cancellationToken);
    }
}
