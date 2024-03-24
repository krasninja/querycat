using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SetKeysRowsInput : RowsInput, IRowsInputKeys
{
    private SelectInputKeysConditions[] _conditions = Array.Empty<SelectInputKeysConditions>();
    private readonly IRowsInputKeys _rowsInput;
    private readonly SelectQueryConditions _selectQueryConditions;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(SetKeysRowsInput));

    /// <inheritdoc />
    public override QueryContext QueryContext
    {
        get => _rowsInput.QueryContext;
        set => _rowsInput.QueryContext = value;
    }

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; }

    public IRowsInputKeys InnerRowsInput => _rowsInput;

    public SetKeysRowsInput(IRowsInputKeys rowsInput, SelectQueryConditions conditions)
    {
        Columns = rowsInput.Columns;
        _rowsInput = rowsInput;
        _selectQueryConditions = conditions;
    }

    /// <inheritdoc />
    public override void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public override void Close() => _rowsInput.Close();

    /// <inheritdoc />
    public override void Reset()
    {
        _rowsInput.Reset();
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();

        foreach (var condition in _conditions)
        {
            foreach (var inputKeyCondition in condition.Conditions)
            {
                var value = inputKeyCondition.ValueFunc.Invoke();
                _rowsInput.SetKeyColumnValue(condition.KeyColumn.ColumnIndex, value, inputKeyCondition.Operation);
            }
        }

        return _rowsInput.ReadNext();
    }

    /// <inheritdoc />
    protected override void Load()
    {
        _conditions = _selectQueryConditions.GetConditionsColumns(_rowsInput).ToArray();
        base.Load();
    }

    /// <inheritdoc />
    public override void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("SetKeys", _rowsInput);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _rowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        // Do not passthru set key value calls.
    }
}
