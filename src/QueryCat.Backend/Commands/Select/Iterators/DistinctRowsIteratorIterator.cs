using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator eliminates duplicated rows.
/// </summary>
internal class DistinctRowsIteratorIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private readonly IFuncUnit[] _columnsFunctions;
    private readonly HashSet<VariantValueArray> _values = new();

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public DistinctRowsIteratorIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IEnumerable<IFuncUnit> columnsIndexes) : this(thread, rowsIterator, columnsIndexes.ToArray())
    {
    }

    public DistinctRowsIteratorIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        params IFuncUnit[] columnsIndexes)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        if (columnsIndexes.Any())
        {
            _columnsFunctions = columnsIndexes;
        }
        else
        {
            // If no columns specified distinct by all columns.
            _columnsFunctions = rowsIterator.Columns
                .Select((_, i) => new FuncUnitRowsIteratorColumn(rowsIterator, i))
                .ToArray();
        }
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (_rowsIterator.MoveNext())
        {
            var values = new VariantValue[_columnsFunctions.Length];
            for (var i = 0; i < _columnsFunctions.Length; i++)
            {
                values[i] = _columnsFunctions[i].Invoke(_thread);
            }
            var arr = new VariantValueArray(values);

            if (_values.Add(arr))
            {
                return true;
            }
        }
        return false;
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

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
