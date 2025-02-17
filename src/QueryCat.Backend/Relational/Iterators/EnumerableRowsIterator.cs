using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Represents as <see cref="IRowsIterator" /> as <see cref="IEnumerable{T}" />.
/// </summary>
public sealed class EnumerableRowsIterator : IAsyncEnumerable<Row>, IRowsSchema
{
    private readonly IRowsIterator _rowsIterator;
    private readonly bool _copyRow;

    /// <summary>
    /// Source rows iterator.
    /// </summary>
    public IRowsIterator RowsIterator => _rowsIterator;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator.</param>
    /// <param name="copyRow">Copy to the new row on each iteration.</param>
    public EnumerableRowsIterator(IRowsIterator rowsIterator, bool copyRow = true)
    {
        _rowsIterator = rowsIterator;
        _copyRow = copyRow;
    }

    /// <inheritdoc />
    public async IAsyncEnumerator<Row> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            if (_copyRow)
            {
                yield return new Row(_rowsIterator.Current);
            }
            else
            {
                yield return _rowsIterator.Current;
            }
        }
    }
}
