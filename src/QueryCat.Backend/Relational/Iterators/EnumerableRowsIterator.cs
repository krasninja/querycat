using System.Collections;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Represents iterator as enumerable of <see cref="Row" />.
/// </summary>
public sealed class EnumerableRowsIterator : IRowsIterator, IRowsIteratorParent, IEnumerable<Row>
{
    private readonly IRowsIterator _rowsIterator;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public EnumerableRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public bool MoveNext() => _rowsIterator.MoveNext();

    /// <inheritdoc />
    public void Reset() => _rowsIterator.MoveNext();

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIterator(_rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerator<Row> GetEnumerator()
    {
        while (MoveNext())
        {
            yield return new Row(Current);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
