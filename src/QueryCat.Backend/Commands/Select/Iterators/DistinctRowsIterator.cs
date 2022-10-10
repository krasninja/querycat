using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator eliminates duplicated rows.
/// </summary>
public class DistinctRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly HashSet<Row> _values = new();

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public DistinctRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        bool hasData;
        while ((hasData = _rowsIterator.MoveNext()) && _values.Contains(_rowsIterator.Current))
        {
        }
        if (hasData)
        {
            _values.Add(new Row(_rowsIterator.Current));
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _values.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Distinct", _rowsIterator);
    }
}
