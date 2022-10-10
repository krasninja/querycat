using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator uses first 10 (default) rows to adjust columns lengths
/// appropriately to the content.
/// </summary>
public class AdjustColumnsLengthsIterator : IRowsIterator
{
    private const int MaxRowsToAnalyze = 10;

    private readonly CacheRowsIterator _rowsIterator;
    private bool _isInitialized;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public AdjustColumnsLengthsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = new CacheRowsIterator(rowsIterator, MaxRowsToAnalyze);
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
            if (Columns[i].Length < Columns[i].Name.Length)
            {
                Columns[i].Length = Columns[i].Name.Length;
            }
        }

        while (_rowsIterator.MoveNext() && _rowsIterator.Position < MaxRowsToAnalyze)
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
}
