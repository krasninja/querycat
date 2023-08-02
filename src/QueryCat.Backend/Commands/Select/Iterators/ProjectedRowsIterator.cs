using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator implements relational projection from
/// one rows set to another one.
/// </summary>
internal sealed class ProjectedRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private Row _currentRow;
    private Column[] _columns = Array.Empty<Column>();
    private IFuncUnit[] _funcUnits = Array.Empty<IFuncUnit>();

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public Row Current => _currentRow;

    public ProjectedRowsIterator(IRowsIterator rowsIterator)
    {
        _rowsIterator = rowsIterator;
        _currentRow = new Row(this);
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var moveResult = _rowsIterator.MoveNext();
        if (moveResult)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                _currentRow[i] = _funcUnits[i].Invoke();
            }
        }
        return moveResult;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    public int AddFuncColumn(Column column, IFuncUnit func)
    {
        Array.Resize(ref _columns, _columns.Length + 1);
        Array.Resize(ref _funcUnits, _funcUnits.Length + 1);
        _columns[^1] = column;
        _funcUnits[^1] = func;
        _currentRow = new Row(this);
        return _columns.Length - 1;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Projection (columns={_columns.Length})", _rowsIterator)
            .AppendSubQueriesWithIndent(_funcUnits);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
