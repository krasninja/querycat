using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator eliminates duplicated rows.
/// </summary>
internal sealed class DistinctRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private readonly IFuncUnit[] _columnsFunctions;
    private readonly HashSet<VariantValueArray> _values = new();
    private VariantValue[] _buffer;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public DistinctRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IEnumerable<IFuncUnit> columnsFunctions) : this(thread, rowsIterator, columnsFunctions.ToArray())
    {
    }

    public DistinctRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        params IFuncUnit[] columnsFunctions)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        if (columnsFunctions.Length > 0)
        {
            _columnsFunctions = columnsFunctions;
        }
        else
        {
            // If no columns specified distinct by all columns.
            _columnsFunctions = rowsIterator.Columns
                .Select((_, i) => new FuncUnitRowsIteratorColumn(rowsIterator, i))
                .ToArray();
        }
        _buffer = new VariantValue[_columnsFunctions.Length];
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (await _rowsIterator.MoveNextAsync(cancellationToken))
        {
            for (var i = 0; i < _columnsFunctions.Length; i++)
            {
                _buffer[i] = await _columnsFunctions[i].InvokeAsync(_thread, cancellationToken);
            }
            var arr = new VariantValueArray(_buffer);

            if (!_values.Add(arr))
            {
                continue;
            }

            // It was the new data - create new buffer.
            _buffer = new VariantValue[_columnsFunctions.Length];
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _values.Clear();
        await _rowsIterator.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent($"Distinct (keys={_columnsFunctions.Length})", _rowsIterator)
            .AppendSubQueriesWithIndent(_columnsFunctions);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
        foreach (var funcUnit in _columnsFunctions)
        {
            if (funcUnit is IRowsIteratorParent funcUnitDelegate)
            {
                foreach (var child in funcUnitDelegate.GetChildren())
                {
                    yield return child;
                }
            }
        }
    }
}
