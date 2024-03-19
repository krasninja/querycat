using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SetKeysRowsInput : IRowsInputKeys
{
    private readonly IRowsInputKeys _rowsInput;
    private readonly SelectQueryConditions _conditions;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsInput.QueryContext;
        set => _rowsInput.QueryContext = value;
    }

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    public SetKeysRowsInput(IRowsInputKeys rowsInput, SelectQueryConditions conditions)
    {
        _rowsInput = rowsInput;
        _conditions = conditions;
    }

    /// <inheritdoc />
    public void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public void Close() => _rowsInput.Close();

    /// <inheritdoc />
    public void Reset() => _rowsInput.Reset();

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public bool ReadNext()
    {
        foreach (var inputKeyCondition in _conditions)
        {
            if (Array.IndexOf(Columns, inputKeyCondition.Column) == -1)
            {
                continue;
            }
            var value = inputKeyCondition.ValueFunc.Invoke();
            _rowsInput.SetKeyColumnValue(
                inputKeyCondition.Column.Name, value, inputKeyCondition.Operation);
        }
        return _rowsInput.ReadNext();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("SetKeys", _rowsInput);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _rowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
        => _rowsInput.SetKeyColumnValue(columnName, value, operation);
}
