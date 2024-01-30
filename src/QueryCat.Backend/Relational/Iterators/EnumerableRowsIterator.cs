using System.Collections;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// Represents as <see cref="IRowsIterator" /> as <see cref="IEnumerable{T}" />.
/// </summary>
/// <param name="rowsIterator">Rows iterator.</param>
public sealed class EnumerableRowsIterator(IRowsIterator rowsIterator) : IEnumerable<Row>
{
    /// <inheritdoc />
    public IEnumerator<Row> GetEnumerator()
    {
        while (rowsIterator.MoveNext())
        {
            yield return new Row(rowsIterator.Current);
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
