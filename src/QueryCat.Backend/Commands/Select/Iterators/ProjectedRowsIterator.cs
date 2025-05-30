using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator implements relational projection from
/// one rows set to another one.
/// </summary>
internal sealed class ProjectedRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly int _id = IdGenerator.GetNext();

    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private Row _currentRow;
    private Column[] _columns = [];
    private IFuncUnit[] _funcUnits = [];

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public Row Current => _currentRow;

    public ProjectedRowsIterator(IExecutionThread thread, IRowsIterator rowsIterator)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        _currentRow = new Row(this);
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var moveResult = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (moveResult)
        {
            for (var i = 0; i < _funcUnits.Length; i++)
            {
                _currentRow[i] = await _funcUnits[i].InvokeAsync(_thread, cancellationToken);
            }
        }
        return moveResult;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _rowsIterator.ResetAsync(cancellationToken);
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
        stringBuilder.AppendRowsIteratorsWithIndent($"Projection (columns={_columns.Length}, id={_id})", _rowsIterator)
            .AppendSubQueriesWithIndent(_funcUnits);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
