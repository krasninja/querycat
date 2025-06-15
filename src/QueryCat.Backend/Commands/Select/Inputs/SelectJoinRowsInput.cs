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
            _rightInputColumnsOffset = rightInput.Columns.Length;
            Columns = rightInput.Columns.Union(leftInput.Columns).ToArray();
        }
        else
        {
            _rightInputColumnsOffset = leftInput.Columns.Length;
            Columns = leftInput.Columns.Union(rightInput.Columns).ToArray();
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
        if (_reverseColumnsOrder)
        {
            if (columnIndex >= _rightInputColumnsOffset)
            {
                columnIndex -= _rightInputColumnsOffset;
            }
            else
            {
                columnIndex += _rightInputColumnsOffset;
            }
        }

        if (columnIndex >= _rightInputColumnsOffset)
        {
            if (_rightIsNull)
            {
                value = VariantValue.Null;
                return ErrorCode.OK;
            }
            return _rightInput.ReadValue(columnIndex - _rightInputColumnsOffset, out value);
        }
        else
        {
            if (_leftIsNull)
            {
                value = VariantValue.Null;
                return ErrorCode.OK;
            }
            return _leftInput.ReadValue(columnIndex, out value);
        }
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

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        return new[] { _leftInput, _rightInput }
            .SelectMany(i => i.GetKeyColumns())
            .ToArray();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        _leftInput.SetKeyColumnValue(columnIndex, value, operation);
        _rightInput.SetKeyColumnValue(columnIndex, value, operation);
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        _leftInput.UnsetKeyColumnValue(columnIndex, operation);
        _rightInput.UnsetKeyColumnValue(columnIndex, operation);
    }

    /// <inheritdoc />
    public override string ToString() => $"left: {_leftInput}, type: {_joinType}, right: {_rightInput}";
}
