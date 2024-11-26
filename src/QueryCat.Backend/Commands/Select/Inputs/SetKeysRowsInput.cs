using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SetKeysRowsInput : RowsInput, IRowsInputKeys
{
    private record struct ConditionJoint(SelectQueryCondition Condition, int ColumnIndex);

    private readonly int _id = IdGenerator.GetNext();

    private ConditionJoint[] _conditions = [];
    private readonly IExecutionThread _thread;
    private readonly IRowsInputKeys _rowsInput;
    private readonly SelectQueryConditions _selectQueryConditions;
    private bool _keysFilled;
    private int _currentFuncValueIndex = -1;
    // Special mode for "WHERE id IN (x, y, z)" condition.
    private ConditionJoint? _multipleConditionJoint;
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

    public SetKeysRowsInput(IExecutionThread thread, IRowsInputKeys rowsInput, SelectQueryConditions conditions)
    {
        Columns = rowsInput.Columns;
        _thread = thread;
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
        _currentFuncValueIndex = -1;
        _keysFilled = false;
        _rowsInput.Reset();
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();

        if (!_keysFilled)
        {
            FillKeys();
            _keysFilled = true;
        }

        var hasData = _rowsInput.ReadNext();

        // We need to repeat "ReadNext" call in case of multiple func values.
        if (_multipleConditionJoint.HasValue && !hasData)
        {
            _rowsInput.Reset();
            hasData = FillKeys();
            if (!hasData)
            {
                return false;
            }
            hasData = _rowsInput.ReadNext();
        }

        return hasData;
    }

    private bool FillKeys()
    {
        if (_multipleConditionJoint.HasValue)
        {
            var condition = _multipleConditionJoint.Value;
            _currentFuncValueIndex++;
            var valueFuncs = condition.Condition.ValueFuncs;
            if (_currentFuncValueIndex >= valueFuncs.Count)
            {
                return false;
            }
            var value = valueFuncs[_currentFuncValueIndex].Invoke(_thread);
            _rowsInput.SetKeyColumnValue(condition.ColumnIndex, value, condition.Condition.Operation);
        }

        foreach (var conditionJoint in _conditions)
        {
            if (conditionJoint == _multipleConditionJoint)
            {
                continue;
            }
            var value = conditionJoint.Condition.ValueFunc.Invoke(_thread);
            _rowsInput.SetKeyColumnValue(conditionJoint.ColumnIndex, value, conditionJoint.Condition.Operation);
        }

        return true;
    }

    /// <inheritdoc />
    protected override void Load()
    {
        // Find all related conditions.
        var list = new List<ConditionJoint>();
        foreach (var condition in _selectQueryConditions.GetConditionsColumns(_rowsInput))
        {
            foreach (var inputKeyCondition in condition.Conditions)
            {
                list.Add(new ConditionJoint(inputKeyCondition, condition.KeyColumn.ColumnIndex));
            }
        }
        _conditions = list.ToArray();

        // Special mode for "WHERE id IN (x, y, z)" condition.
        var multipleConditions = _conditions
            .Where(c => c.Condition.Operation == VariantValue.Operation.Equals && c.Condition.ValueFuncs.Count > 0)
            .ToArray();
        _multipleConditionJoint = multipleConditions.Length == 1 ? multipleConditions[0] : null;

        base.Load();
    }

    /// <inheritdoc />
    public override void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent($"SetKeys (id={_id})", _rowsInput);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _rowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        // Do not passthru set key value calls.
    }

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}, RowsInput = {_rowsInput.GetType().Name}";
}
