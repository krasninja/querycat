using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Prefetches the values. It is used to fill rows iterator with values from input.
/// The main reason to use is to avoid reading (and converting) the values we are not going to process.
/// </summary>
internal sealed class PrefetchRowsIterator : IRowsIterator
{
    private readonly RowsInputIterator _rowsInputIterator;
    private readonly IRowsIterator _rowsIterator;
    private readonly int[] _columnIds;
    private readonly Row _row;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _row;

    public PrefetchRowsIterator(
        IRowsIterator rowsIterator,
        RowsInputIterator rowsInputIterator,
        int[] columnIds)
    {
        _rowsInputIterator = rowsInputIterator;
        _rowsIterator = rowsIterator;
        _columnIds = columnIds;

        Columns = columnIds.Select(id => _rowsInputIterator.Columns[id]).ToArray();
        _row = new Row(Columns);
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var hasData = _rowsIterator.MoveNext();
        if (hasData)
        {
            _rowsInputIterator.FetchValuesForColumns(_columnIds);
            for (var i = 0; i < _columnIds.Length; i++)
            {
                _row[i] = _rowsInputIterator.Current[_columnIds[i]];
            }
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Prefetch {string.Join(", ", _columnIds)}", _rowsIterator);
    }
}
