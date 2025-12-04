using System.Globalization;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator uses first 10 (default) rows to adjust columns lengths
/// appropriately to the content.
/// </summary>
public class AdjustColumnsLengthsIterator : IRowsIterator, IRowsIteratorParent
{
    private const int MaxRowsToAnalyze = 10;

    private readonly IRowsIterator _rowsIterator;
    private readonly int _maxRowsToAnalyze;
    private readonly CacheRowsIterator _cacheRowsIterator;
    private bool _isInitialized;
    private bool _cacheMode;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _cacheMode ? _cacheRowsIterator.Current : _rowsIterator.Current;

    public AdjustColumnsLengthsIterator(IRowsIterator rowsIterator, int maxRowsToAnalyze = MaxRowsToAnalyze)
    {
        _rowsIterator = rowsIterator;
        _maxRowsToAnalyze = maxRowsToAnalyze;
        _cacheRowsIterator = new CacheRowsIterator(rowsIterator);
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        if (!_cacheRowsIterator.EndOfCache)
        {
            var hasData = await _cacheRowsIterator.MoveNextAsync(cancellationToken);
            if (hasData)
            {
                _cacheMode = true;
                return hasData;
            }
        }

        _cacheMode = false;
        return await _rowsIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _cacheRowsIterator.ResetAsync(cancellationToken);
        _isInitialized = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Adj Columns", _cacheRowsIterator);
    }

    private async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        SetColumnsWidthMatchNames();

        while (await _cacheRowsIterator.MoveNextAsync(cancellationToken) && _cacheRowsIterator.Position < _maxRowsToAnalyze)
        {
            for (var i = 0; i < Columns.Length; i++)
            {
                var internalType = _cacheRowsIterator.Current[i].Type;
                if (internalType == DataType.Void || internalType == DataType.Object)
                {
                    continue;
                }
                var value = _cacheRowsIterator.Current[i].ToString(CultureInfo.InvariantCulture);
                if (value.Length > Columns[i].Length)
                {
                    Columns[i].Length = value.Length;
                }
            }
        }
        _isInitialized = true;
        _cacheRowsIterator.SeekCacheCursorToHead();
        _cacheRowsIterator.Freeze();

        SetColumnsWidthMatchNames();
    }

    private void SetColumnsWidthMatchNames()
    {
        foreach (var column in Columns)
        {
            if (column.Length < column.FullName.Length)
            {
                column.Length = column.FullName.Length;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _cacheRowsIterator;
    }
}
