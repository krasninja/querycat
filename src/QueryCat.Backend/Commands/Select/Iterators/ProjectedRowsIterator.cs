using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator implements relational projection from
/// one rows set to another one.
/// </summary>
internal sealed class ProjectedRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private Row _currentRow;
    private FuncColumn[] _columns = Array.Empty<FuncColumn>();

    /// <inheritdoc />
    public Column[] Columns => _columns;

    /// <inheritdoc />
    public Row Current => _currentRow;

    internal sealed class FuncColumn : Column
    {
        public FuncUnit Func { get; }

        /// <inheritdoc />
        public FuncColumn(Column column, FuncUnit func)
            : base(column)
        {
            Func = func;
        }

        public VariantValue GetValue() => Func.Invoke();
    }

    public ProjectedRowsIterator(IRowsIterator rowsIterator, ColumnsInfoContainer columnsInfoContainer)
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
                _currentRow[i] = _columns[i].GetValue();
            }
        }
        return moveResult;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
    }

    public int AddFuncColumn(Column column, FuncUnit func)
    {
        Array.Resize(ref _columns, _columns.Length + 1);
        _columns[^1] = new FuncColumn(column, func);
        _currentRow = new Row(this);
        return _columns.Length - 1;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Projection", _rowsIterator)
            .AppendSubQueriesWithIndent(_columns.Select(c => c.Func));
    }
}
