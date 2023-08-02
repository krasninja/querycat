using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Adds row number column into target rows set.
/// </summary>
internal sealed class RowIdRowsIterator : IRowsIterator, IRowsIteratorParent
{
    public const string ColumName = "row_number";

    private readonly IRowsIterator _rowsIterator;
    private readonly Row _row;
    private long _rowId;

    /// <inheritdoc />
    public Column[] Columns { get; }

    public RowIdRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        Columns = new[]
        {
            new Column(ColumName, DataType.Integer, "Row number.")
        }.Union(rowsIterator.Columns).ToArray();
        _row = new Row(this);
    }

    /// <inheritdoc />
    public Row Current
    {
        get
        {
            var current = _rowsIterator.Current;
            for (int i = 0; i < current.Columns.Length; i++)
            {
                _row[i + 1] = current[i];
            }
            _row[0] = new VariantValue(_rowId);
            return _row;
        }
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var hasData = _rowsIterator.MoveNext();
        if (hasData)
        {
            _rowId++;
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _rowId = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Row Id", _rowsIterator);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
