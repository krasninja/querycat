using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands.Select.KeyConditionValue;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SetKeysRowsInput : IRowsInputUpdate, IRowsInputDelete
{
    private sealed record ConditionJoint(
        SelectQueryCondition Condition,
        int ColumnIndex)
    {
        public VariantValue? KeyValue { get; set; }
    }

    private readonly int _id = IdGenerator.GetNext();

    private ConditionJoint[] _conditions = [];
    private readonly IExecutionThread _thread;
    private readonly IRowsInput _rowsInput;
    private readonly SelectQueryConditions _selectQueryConditions;
    private bool _keysFilled;
    // Special mode for "WHERE id IN (x, y, z)" condition.
    private bool _hasMultipleConditions;
    private bool _hasNoMoreData;
    private bool _needFillConditions = true;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(SetKeysRowsInput));

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsInput.QueryContext;
        set => _rowsInput.QueryContext = value;
    }

    /// <inheritdoc />
    public Column[] Columns { get; }

    public IRowsInput InnerRowsInput => _rowsInput;

    public SetKeysRowsInput(IExecutionThread thread, IRowsInput rowsInput, SelectQueryConditions conditions)
    {
        Columns = rowsInput.Columns;
        _thread = thread;
        _rowsInput = rowsInput;
        _selectQueryConditions = conditions;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _rowsInput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _rowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
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
    public string[] UniqueKey => _rowsInput.UniqueKey;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        // Fast path.
        if (_hasNoMoreData)
        {
            return false;
        }
        if (_needFillConditions)
        {
            FillConditions();
            _needFillConditions = false;
        }
        if (_conditions.Length == 0)
        {
            return await _rowsInput.ReadNextAsync(cancellationToken);
        }

        // Fill conditions and check them.
        var keysValid = false;
        while (!keysValid)
        {
            var hasData = await ReadNextInternalAsync(cancellationToken);
            if (!hasData)
            {
                break;
            }
            keysValid = CheckConditions();
            if (keysValid)
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<bool> ReadNextInternalAsync(CancellationToken cancellationToken)
    {
        if (_conditions.Length > 0 && !_keysFilled)
        {
            await FillKeysAsync(cancellationToken);
            _keysFilled = true;
        }

        var hasData = await _rowsInput.ReadNextAsync(cancellationToken);

        // We need to repeat "ReadNext" call in case of multiple func values.
        if (_conditions.Length > 0 && _hasMultipleConditions && !hasData)
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
            conditionJoint.KeyValue = await conditionJoint.Condition.Generator.GetAsync(_thread, cancellationToken);
            if (conditionJoint.KeyValue.HasValue)
            {
                _rowsInput.SetKeyColumnValue(conditionJoint.ColumnIndex, conditionJoint.KeyValue.Value, conditionJoint.Condition.Operation);
            }
            else
            {
                _rowsInput.UnsetKeyColumnValue(conditionJoint.ColumnIndex, conditionJoint.Condition.Operation);
            }
        }

        return hasMoreMultipleValues;
    }

    private void FillConditions()
    {
        // Find all related conditions.
        var list = new List<ConditionJoint>();
        foreach (var condition in _selectQueryConditions.GetConditionsColumns(_rowsInput))
        {
            foreach (var inputKeyCondition in condition.Conditions)
            {
                var conditionJoint = new ConditionJoint(
                    inputKeyCondition,
                    condition.KeyColumn.ColumnIndex);
                list.Add(conditionJoint);
            }
        }
        _conditions = list.ToArray();

        // Special mode for "WHERE id IN (x, y, z)" condition.
        _hasMultipleConditions = _conditions
            .Any(c => c.Condition.Operation == VariantValue.Operation.Equals
                        && c.Condition.Generator is IKeyConditionMultipleValuesGenerator);
    }

    /*
     * We can check certain condition here to avoid unnecessary data reads. Example:
     * "select * from x() inner join y() on x.id = y.id where x like '%cat%'"
     * By current logic we will fetch all y() related records and only after that check for pattern match.
     * With this check we verify conditions before y() fetch and reduce data load:
     * 1. load x() rows;
     * 2. filter x() rows; <-
     * 3. load all related y() rows;
     * 4. filter whole row by pattern;
     */

    private bool CheckConditions()
    {
        foreach (var condition in _conditions)
        {
            if (!condition.KeyValue.HasValue || !IsLogicalOperation(condition.Condition.Operation))
            {
                continue;
            }
            var errorCode = ReadValue(condition.ColumnIndex, out var columnValue);
            if (errorCode != ErrorCode.OK)
            {
                continue;
            }
            var operationDelegate = VariantValue.GetOperationDelegate(
                condition.Condition.Operation, columnValue.Type, condition.KeyValue.Value.Type);
            var matchCondition = operationDelegate.Invoke(columnValue, condition.KeyValue.Value);
            if (!matchCondition.AsBoolean)
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> UpdateValueAsync(int columnIndex, VariantValue value, CancellationToken cancellationToken = default)
    {
        if (_rowsInput is not IRowsInputUpdate rowsInputUpdate)
        {
            return ErrorCode.NotSupported;
        }
        return await rowsInputUpdate.UpdateValueAsync(columnIndex, value, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_rowsInput is not IRowsInputDelete rowsInputDelete)
        {
            return ErrorCode.NotSupported;
        }
        return await rowsInputDelete.DeleteAsync(cancellationToken);
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
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent($"SetKeys (id={_id})", _rowsInput);
    }

    private static bool IsLogicalOperation(VariantValue.Operation operation)
        => operation == VariantValue.Operation.Equals
           || operation == VariantValue.Operation.NotEquals
           || operation == VariantValue.Operation.And
           || operation == VariantValue.Operation.Or
           || operation == VariantValue.Operation.GreaterOrEquals
           || operation == VariantValue.Operation.Greater
           || operation == VariantValue.Operation.LessOrEquals
           || operation == VariantValue.Operation.Less
           || operation == VariantValue.Operation.Like
           || operation == VariantValue.Operation.Between
           || operation == VariantValue.Operation.BetweenAnd
           || operation == VariantValue.Operation.IsNotNull
           || operation == VariantValue.Operation.IsNull
           || operation == VariantValue.Operation.NotLike
           || operation == VariantValue.Operation.NotSimilar;

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}, RowsInput = {_rowsInput.GetType().Name}";
}
