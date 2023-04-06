using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator maps input columns into another set of columns.
/// </summary>
public sealed class MapRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    // ReSharper disable once UseArrayEmptyMethod
    private int[] _mapping = new int[0];
    // ReSharper disable once UseArrayEmptyMethod
    private Column[] _columns = new Column[0];
    private Row _row;

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public Row Current => _row;

    public MapRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        _row = new Row(_columns);
    }

    /// <summary>
    /// Add column for source rows iterator to target.
    /// </summary>
    /// <param name="index">Source column index.</param>
    /// <returns>Instance of <see cref="MapRowsIterator" />.</returns>
    public MapRowsIterator Add(int index)
    {
        Array.Resize(ref _mapping, _mapping.Length + 1);
        Array.Resize(ref _columns, _columns.Length + 1);
        _mapping[^1] = index;
        _columns[^1] = _rowsIterator.Columns[index];
        _row = new Row(_columns);
        return this;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var hasData = _rowsIterator.MoveNext();
        for (var i = 0; i < _mapping.Length; i++)
        {
            _row[i] = _rowsIterator.Current[_mapping[i]];
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
        stringBuilder.AppendRowsIteratorsWithIndent("Map", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
