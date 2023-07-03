using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal class SetKeyColumnsRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly SelectInputKeysConditions[] _inputKeysConditions;
    private bool _setColumns = true;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public SetKeyColumnsRowsIterator(
        IRowsIterator rowsIterator,
        SelectInputKeysConditions[] inputKeysConditions)
    {
        _rowsIterator = rowsIterator;
        _inputKeysConditions = inputKeysConditions;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_setColumns)
        {
            foreach (var inputKeyCondition in _inputKeysConditions)
            {
                var condition = inputKeyCondition.Conditions.FirstOrDefault();
                if (condition != null)
                {
                    inputKeyCondition.RowsInput.SetKeyColumnValue(
                        inputKeyCondition.KeyColumn.ColumnName, condition.ValueFunc.Invoke());
                }
            }
            _setColumns = false;
        }
        return _rowsIterator.MoveNext();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _setColumns = true;
        _rowsIterator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("SetKeys", _rowsIterator);
    }
}
