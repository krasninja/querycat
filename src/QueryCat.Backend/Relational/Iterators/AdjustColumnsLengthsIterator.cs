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

    private readonly int _maxRowsToAnalyze;
    private readonly CacheRowsIterator _cacheRowsIterator;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _cacheRowsIterator.Current;

    public AdjustColumnsLengthsIterator(IRowsIterator rowsIterator, int maxRowsToAnalyze = MaxRowsToAnalyze)
    {
        _maxRowsToAnalyze = maxRowsToAnalyze;
        _cacheRowsIterator = new CacheRowsIterator(rowsIterator);
        Columns = rowsIterator.Columns;
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }

        return await _cacheRowsIterator.MoveNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _cacheRowsIterator.Reset();
        _isInitialized = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Adj Columns", _cacheRowsIterator);
    }

    private async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (var column in Columns)
        {
            if (column.Length < column.FullName.Length)
            {
                column.Length = column.FullName.Length;
            }
        }

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
        _cacheRowsIterator.Seek(-1, CursorSeekOrigin.Begin);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _cacheRowsIterator;
    }
}
