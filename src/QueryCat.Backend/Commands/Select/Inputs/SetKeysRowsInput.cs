using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands.Select.KeyConditionValue;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SetKeysRowsInput : RowsInput, IRowsInputKeys
{
    private sealed record ConditionJoint(
        SelectQueryCondition Condition,
        int ColumnIndex);

    private readonly int _id = IdGenerator.GetNext();

    private ConditionJoint[] _conditions = [];
    private readonly IExecutionThread _thread;
    private readonly IRowsInputKeys _rowsInput;
    private readonly SelectQueryConditions _selectQueryConditions;
    private bool _keysFilled;
    // Special mode for "WHERE id IN (x, y, z)" condition.
    private bool _hasMultipleConditions;
    private bool _hasNoMoreData;
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
    public override Task OpenAsync(CancellationToken cancellationToken = default) => _rowsInput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default) => _rowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        foreach (var condition in _conditions)
        {
            if (condition.Condition.Generator is IKeyConditionMultipleValuesGenerator multipleValuesGenerator)
            {
                await multipleValuesGenerator.ResetAsync(cancellationToken);
            }
        }
        _keysFilled = false;
        _hasNoMoreData = false;
        await _rowsInput.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public override async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_hasNoMoreData)
        {
            return false;
        }
        await base.ReadNextAsync(cancellationToken);

        if (!_keysFilled)
        {
            await FillKeysAsync(cancellationToken);
            _keysFilled = true;
        }

        var hasData = await _rowsInput.ReadNextAsync(cancellationToken);

        // We need to repeat "ReadNext" call in case of multiple func values.
        if (_hasMultipleConditions && !hasData)
        {
            await _rowsInput.ResetAsync(cancellationToken);
            hasData = await FillKeysAsync(cancellationToken);
            if (!hasData)
            {
                _hasNoMoreData = true;
                return false;
            }
            hasData = await _rowsInput.ReadNextAsync(cancellationToken);
        }

        return hasData;
    }

    private async ValueTask<bool> FillKeysAsync(CancellationToken cancellationToken)
    {
        var hasMoreMultipleValues = false;
        foreach (var conditionJoint in _conditions)
        {
            if (!hasMoreMultipleValues
                && conditionJoint.Condition.Generator is IKeyConditionMultipleValuesGenerator multipleValuesGenerator)
            {
                hasMoreMultipleValues = await multipleValuesGenerator.MoveNextAsync(_thread, cancellationToken);
            }
            var nullableValue = await conditionJoint.Condition.Generator.GetAsync(_thread, cancellationToken);
            if (nullableValue.HasValue)
            {
                _rowsInput.SetKeyColumnValue(conditionJoint.ColumnIndex, nullableValue.Value, conditionJoint.Condition.Operation);
            }
            else
            {
                _rowsInput.UnsetKeyColumnValue(conditionJoint.ColumnIndex, conditionJoint.Condition.Operation);
            }
        }

        return hasMoreMultipleValues;
    }

    /// <inheritdoc />
    protected override ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        // Find all related conditions.
        var list = new List<ConditionJoint>();
        foreach (var condition in _selectQueryConditions.GetConditionsColumns(_rowsInput))
        {
            foreach (var inputKeyCondition in condition.Conditions)
            {
                list.Add(new ConditionJoint(
                    inputKeyCondition,
                    condition.KeyColumn.ColumnIndex));
            }
        }
        _conditions = list.ToArray();

        // Special mode for "WHERE id IN (x, y, z)" condition.
        _hasMultipleConditions = _conditions
            .Any(c => c.Condition.Operation == VariantValue.Operation.Equals
                        && c.Condition.Generator is IKeyConditionMultipleValuesGenerator);

        return base.LoadAsync(cancellationToken);
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
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        // Do not passthru key value calls.
    }

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}, RowsInput = {_rowsInput.GetType().Name}";
}
