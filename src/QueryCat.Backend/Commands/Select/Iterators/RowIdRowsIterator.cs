using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// Adds row number column into target rows set.
/// </summary>
internal sealed class RowIdRowsIterator : IRowsIterator
{
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
            new Column("row_number", DataType.Integer, "Row number")
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
            _row[0] = new VariantValue(_rowId++);
            return _row;
        }
    }

    /// <inheritdoc />
    public bool MoveNext() => _rowsIterator.MoveNext();

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
}
