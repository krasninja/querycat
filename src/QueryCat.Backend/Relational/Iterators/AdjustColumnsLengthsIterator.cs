using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator uses first 10 (default) rows to adjust columns lengths
/// appropriately to the content.
/// </summary>
internal class AdjustColumnsLengthsIterator : IRowsIterator, IRowsIteratorParent
{
    private const int MaxRowsToAnalyze = 10;

    private readonly int _maxRowsToAnalyze;
    private readonly CacheRowsIterator _rowsIterator;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public AdjustColumnsLengthsIterator(IRowsIterator rowsIterator, int maxRowsToAnalyze = MaxRowsToAnalyze)
    {
        _maxRowsToAnalyze = maxRowsToAnalyze;
        _rowsIterator = new CacheRowsIterator(rowsIterator);
        Columns = rowsIterator.Columns;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _rowsIterator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        ((IRowsIterator)_rowsIterator).Reset();
        _isInitialized = false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Adj Columns", _rowsIterator);
    }

    private void Initialize()
    {
        for (var i = 0; i < Columns.Length; i++)
        {
            if (Columns[i].Length < Columns[i].FullName.Length)
            {
                Columns[i].Length = Columns[i].FullName.Length;
            }
        }

        while (_rowsIterator.MoveNext() && _rowsIterator.Position < _maxRowsToAnalyze)
        {
            for (var i = 0; i < Columns.Length; i++)
            {
                var internalType = _rowsIterator.Current[i].GetInternalType();
                if (internalType == DataType.Void || internalType == DataType.Object)
                {
                    continue;
                }
                var value = _rowsIterator.Current[i].ToString();
                if (value.Length > Columns[i].Length)
                {
                    Columns[i].Length = value.Length;
                }
            }
        }
        _isInitialized = true;
        _rowsIterator.Seek(-1, CursorSeekOrigin.Begin);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
