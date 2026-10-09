using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SelectJoinRowsInput : IRowsInput, IRowsIteratorParent
{
    private readonly IExecutionThread _thread;
    private readonly IRowsInput _leftInput;
    private readonly IRowsInput _rightInput;
    private readonly JoinType _joinType;
    private readonly IFuncUnit _condition;
    private readonly bool _reverseColumnsOrder;
    private readonly int _leftInputColumnsOffset;
    private readonly int _rightInputColumnsOffset;
    private bool _rightHasData;
    private bool _rightIsNull;
    private bool _leftIsNull;
    private readonly HashSet<long> _fullJoinRightIncludes = new();
    private long _rightRowIndex = -1;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey => _leftInput.UniqueKey.Concat(_rightInput.UniqueKey).ToArray();

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _leftInput.QueryContext;
        set
        {
            _leftInput.QueryContext = value;
            _rightInput.QueryContext = value;
        }
    }

    public SelectJoinRowsInput(
        IExecutionThread thread,
        IRowsInput leftInput,
        IRowsInput rightInput,
        JoinType joinType,
        IFuncUnit condition,
        bool reverseColumnsOrder = false)
    {
        _thread = thread;
        _leftInput = leftInput;
        _rightInput = rightInput;
        _joinType = joinType;
        _condition = condition;
        _reverseColumnsOrder = reverseColumnsOrder;
        if (reverseColumnsOrder)
        {
            _rightInputColumnsOffset = 0;
            _leftInputColumnsOffset = rightInput.Columns.Length;
            Columns = rightInput.Columns.Concat(leftInput.Columns).ToArray();
        }
        else
        {
            _leftInputColumnsOffset = 0;
            _rightInputColumnsOffset = leftInput.Columns.Length;
            Columns = leftInput.Columns.Concat(rightInput.Columns).ToArray();
        }
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await _leftInput.OpenAsync(cancellationToken);
        await _rightInput.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await _leftInput.CloseAsync(cancellationToken);
        await _rightInput.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var (input, inputColumnIndex) = GetInputColumn(columnIndex);
        if (input == _leftInput ? _leftIsNull : _rightIsNull)
        {
            value = VariantValue.Null;
            return ErrorCode.OK;
        }
        return input.ReadValue(inputColumnIndex, out value);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        while (_rightHasData || await _leftInput.ReadNextAsync(cancellationToken))
        {
            while (await _rightInput.ReadNextAsync(cancellationToken))
            {
                _rightRowIndex++;
                var matches = (await _condition.InvokeAsync(_thread, cancellationToken)).AsBoolean;
                if (matches)
                {
                    _rightHasData = true;
                    if (_joinType == JoinType.Full)
                    {
                        _fullJoinRightIncludes.Add(_rightRowIndex);
                    }
                    return true;
                }
            }

            // For reference: https://postgrespro.ru/docs/postgrespro/15/queries-table-expressions?lang=en#QUERIES-FROM.
            if (!_rightHasData && (_joinType == JoinType.Left || _joinType == JoinType.Right
                                                              || _joinType == JoinType.Full))
            {
                _rightIsNull = true;
                _rightHasData = true;
                return true;
            }

            _rightIsNull = false;
            _rightHasData = false;
            await _rightInput.ResetAsync(cancellationToken);
            _rightRowIndex = -1;
        }

        if (_joinType == JoinType.Full)
        {
            _rightHasData = false;
            _rightIsNull = false;
            _leftIsNull = true;
            while (await _rightInput.ReadNextAsync(cancellationToken))
            {
                _rightRowIndex++;
                if (!_fullJoinRightIncludes.Contains(_rightRowIndex))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _leftInput.ResetAsync(cancellationToken);
        await _rightInput.ResetAsync(cancellationToken);
        _rightHasData = false;
        _rightIsNull = false;
        _leftIsNull = false;
        _rightRowIndex = -1;
        _fullJoinRightIncludes.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent($"{_joinType} join", _leftInput, _rightInput);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _leftInput;
        yield return _rightInput;
    }

    /// <summary>
    /// Map the join column index to the source input and its own column index.
    /// </summary>
    /// <param name="columnIndex">Column index within <see cref="Columns" />.</param>
    /// <returns>Source input and column index within it.</returns>
    private (IRowsInput Input, int ColumnIndex) GetInputColumn(int columnIndex)
    {
        var isLeft = _reverseColumnsOrder
            ? columnIndex >= _leftInputColumnsOffset
            : columnIndex < _rightInputColumnsOffset;
        return isLeft
            ? (_leftInput, columnIndex - _leftInputColumnsOffset)
            : (_rightInput, columnIndex - _rightInputColumnsOffset);
    }

    private static KeyColumn ShiftKeyColumn(KeyColumn keyColumn, int offset)
        => offset == 0
            ? keyColumn
            : new KeyColumn(keyColumn.ColumnIndex + offset, keyColumn.IsRequired, keyColumn.GetOperations().ToArray());

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        return _leftInput.GetKeyColumns().Select(k => ShiftKeyColumn(k, _leftInputColumnsOffset))
            .Concat(_rightInput.GetKeyColumns().Select(k => ShiftKeyColumn(k, _rightInputColumnsOffset)))
            .ToArray();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        var (input, inputColumnIndex) = GetInputColumn(columnIndex);
        input.SetKeyColumnValue(inputColumnIndex, value, operation);
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        var (input, inputColumnIndex) = GetInputColumn(columnIndex);
        input.UnsetKeyColumnValue(inputColumnIndex, operation);
    }

    /// <inheritdoc />
    public override string ToString() => $"left: {_leftInput}, type: {_joinType}, right: {_rightInput}";
}
