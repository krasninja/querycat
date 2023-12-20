using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class SelectJoinRowsInput : IRowsInputKeys, IRowsIteratorParent
{
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

    public SelectJoinRowsInput(IRowsInput leftInput, IRowsInput rightInput, JoinType joinType, IFuncUnit condition,
        bool reverseColumnsOrder = false)
    {
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
    public void Open()
    {
        _leftInput.Open();
        _rightInput.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        _leftInput.Close();
        _rightInput.Close();
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
    public bool ReadNext()
    {
        while (_rightHasData || _leftInput.ReadNext())
        {
            while (_rightInput.ReadNext())
            {
                _rightRowIndex++;
                var matches = _condition.Invoke().AsBoolean;
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
            _rightInput.Reset();
            _rightRowIndex = -1;
        }

        if (_joinType == JoinType.Full)
        {
            _rightHasData = false;
            _rightIsNull = false;
            _leftIsNull = true;
            while (_rightInput.ReadNext())
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
    public void Reset()
    {
        _leftInput.Reset();
        _rightInput.Reset();
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
            .OfType<IRowsInputKeys>()
            .SelectMany(i => i.GetKeyColumns())
            .ToArray();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
    {
        if (_leftInput is IRowsInputKeys leftInputKeys)
        {
            leftInputKeys.SetKeyColumnValue(columnName, value, operation);
        }
        if (_rightInput is IRowsInputKeys rightInputKeys)
        {
            rightInputKeys.SetKeyColumnValue(columnName, value, operation);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"left: {_leftInput}, type: {_joinType}, right: {_rightInput}";
}
