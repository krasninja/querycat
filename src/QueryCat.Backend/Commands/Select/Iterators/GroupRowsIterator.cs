using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal sealed class GroupRowsIterator : IRowsIterator, IRowsIteratorParent
{
    private readonly IRowsIterator _rowsIterator;
    private bool _isInitialized;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private readonly int _aggregateColumnsOffset;
    private readonly IFuncUnit[] _keys;
    private readonly SelectCommandContext _context;
    private readonly AggregateTarget[] _targets;

    internal static IFuncUnit[] NoGroupsKeyFactory { get; } =
    {
        new FuncUnitStatic(VariantValue.OneIntegerValue)
    };

    /*
     * For aggregate queries we break pipeline execution and have to prepare new rows frame.
     * We also prepare new columns. For example:
     *
     * Table: id, first, last, balance
     * SELECT sum(balance) FROM tbl GROUP BY first HAVING count(1) > 2;
     *
     * Final aggregate rows frame columns:
     * id, first, last, balance, sum(balance), count(1)
     *
     * 0-3 - the columns copy from input table
     * 4-5 - calculated aggregates
     * aggregateColumnsOffset = 4
     */

    private readonly struct GroupKeyEntry
    {
        public VariantValueArray[] AggregateStates { get; }

        public int RowIndex { get; }

        public GroupKeyEntry(VariantValueArray[] aggregateStates, int rowIndex)
        {
            AggregateStates = aggregateStates;
            RowIndex = rowIndex;
        }

        /// <inheritdoc />
        public override string ToString() => $"{RowIndex}: {AggregateStates}";
    }

    /// <inheritdoc />
    public Column[] Columns => _rowsFrameIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsFrameIterator.Current;

    internal RowsFrame RowsFrame => _rowsFrame;

    public GroupRowsIterator(
        IRowsIterator rowsIterator,
        IFuncUnit[] keys,
        SelectCommandContext context,
        AggregateTarget[] targets)
    {
        _rowsIterator = rowsIterator;
        _keys = keys;
        _context = context;
        _targets = targets;

        var columns = GetAggregateColumns(rowsIterator, targets);
        _aggregateColumnsOffset = rowsIterator.Columns.Length;
        _rowsFrame = new RowsFrame(columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            FillRows();
            _isInitialized = true;
        }

        return _rowsFrameIterator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isInitialized = false;
        _rowsFrame.Clear();
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Group By", _rowsIterator)
            .AppendSubQueriesWithIndent(_keys);
    }

    private void FillRows()
    {
        var keysRowIndexesMap = new Dictionary<VariantValueArray, GroupKeyEntry>(capacity: 1024);

        // Fill keysRowIndexesMap.
        while (_rowsIterator.MoveNext())
        {
            // Format key and fill aggregate values.
            _keys[0].Invoke();
            var keyValues = _keys.Select(f => f.Invoke()).ToArray();
            var key = new VariantValueArray(keyValues);
            if (!keysRowIndexesMap.TryGetValue(key, out GroupKeyEntry groupKey))
            {
                var row = new Row(_rowsFrame);
                for (var i = 0; i < _rowsIterator.Columns.Length; i++)
                {
                    row[i] = _rowsIterator.Current[i];
                }
                VariantValueArray[] initialStates = _targets.Select(t => t.AggregateFunction.GetInitialState(t.ReturnType)).ToArray();
                groupKey = new GroupKeyEntry(initialStates, _rowsFrame.AddRow(row));
                keysRowIndexesMap.Add(key, groupKey);
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                _targets[i].ValueGenerator.Invoke(); // We need this call to fill FunctionCallInfo.
                _targets[i].AggregateFunction.Invoke(groupKey.AggregateStates[i], _targets[i].FunctionCallInfo);
            }
        }

        // Fill rows frame.
        if (keysRowIndexesMap.Any())
        {
            foreach (var mapValue in keysRowIndexesMap)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    var value = _targets[i].AggregateFunction.GetResult(mapValue.Value.AggregateStates[i]);
                    _rowsFrame.UpdateValue(mapValue.Value.RowIndex, _aggregateColumnsOffset + i, value);
                }
            }
        }
        else if (_keys == NoGroupsKeyFactory)
        {
            // If no data at all - we produce default result.
            var defaultValuesRow = new Row(_rowsFrame);
            for (var i = 0; i < _targets.Length; i++)
            {
                var target = _targets[i];
                defaultValuesRow[_aggregateColumnsOffset + i] = target.AggregateFunction.GetResult(
                    target.AggregateFunction.GetInitialState(target.ReturnType));
            }
            _rowsFrame.AddRow(defaultValuesRow);
        }
    }

    private Column[] GetAggregateColumns(IRowsIterator rows, AggregateTarget[] targets)
    {
        var columns = new List<Column>();
        foreach (var rowsColumn in rows.Columns)
        {
            columns.Add(rowsColumn);
        }
        foreach (var target in targets)
        {
            var columnName = !string.IsNullOrEmpty(target.Name) ? target.Name : $"a-{target.Node.Id}";
            var column = new Column(columnName, target.ReturnType);
            columns.Add(column);
            var info = _context.ColumnsInfoContainer.GetByColumn(column);
            info.IsAggregateKey = true;
        }
        return columns.ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
