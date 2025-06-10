using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

internal sealed class VaryingRowsInput : IRowsInputKeys, IRowsInputDelete, IRowsInputUpdate
{
    private readonly IRowsInput _initialRowsInput;
    private readonly QueryContext _queryContext;
    private readonly IExecutionThread<ExecutionOptions> _thread;
    private IRowsInput? _currentInput;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(VaryingRowsInput));

    private readonly FunctionResultStore _store;
    private readonly RowsInputFactory _rowsInputFactory;
    private bool _isEndOfData;

    private IRowsInput RowsInput => _currentInput ?? _initialRowsInput;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => RowsInput.QueryContext;
        set => RowsInput.QueryContext = value;
    }

    /// <inheritdoc />
    public Column[] Columns => RowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => RowsInput.UniqueKey;

    public VaryingRowsInput(
        IExecutionThread<ExecutionOptions> thread,
        IRowsInput initialRowsInput,
        FunctionResultStore store,
        RowsInputFactory rowsInputFactory,
        QueryContext queryContext)
    {
        _thread = thread;
        _initialRowsInput = initialRowsInput;
        _queryContext = queryContext;

        _store = store;
        _rowsInputFactory = rowsInputFactory;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => RowsInput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => RowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => RowsInput.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_currentInput == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        return _currentInput.ReadValue(columnIndex, out value);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        // If end of data we try to read from the current input to support "follow" mode.
        if (_isEndOfData)
        {
            if (_currentInput != null)
            {
                return await _currentInput.ReadNextAsync(cancellationToken);
            }
            return false;
        }

        // Current input is not initialized - get the new one.
        if (_currentInput == null)
        {
            _currentInput = await GetNewRowsInput(cancellationToken);
        }

        var hasData = await _currentInput.ReadNextAsync(cancellationToken);
        if (hasData)
        {
            return true;
        }

        // If no data - reset _currentInput to force getting the new one.
        _currentInput = null;
        return false;
    }

    private async ValueTask<IRowsInput> GetNewRowsInput(CancellationToken cancellationToken)
    {
        var callResult = await _store.CallAsync(_thread, cancellationToken);

        // This is the new input.
        if (!callResult.Cached)
        {
            // Close the current one.
            if (_currentInput != null)
            {
                await _currentInput.CloseAsync(cancellationToken);
            }

            // Create new input for variant value.
            var newInputContext = await _rowsInputFactory.CreateRowsInputAsync(callResult.Value, _thread, cancellationToken);
            if (newInputContext == null)
            {
                _isEndOfData = true;
                return RowsInput;
            }

            // Initialize new input.
            var newInput = newInputContext.RowsInput;
            _currentInput = newInput;
            _currentInput.QueryContext = _queryContext;
            await _currentInput.OpenAsync(cancellationToken);
        }
        // If cached - the same result was used as before. That means we out of data.
        else
        {
            _isEndOfData = true;
        }

        return RowsInput;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Varying", _initialRowsInput);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> UpdateValueAsync(int columnIndex, VariantValue value, CancellationToken cancellationToken = default)
    {
        if (RowsInput is IRowsInputUpdate deleteRows)
        {
            return await deleteRows.UpdateValueAsync(columnIndex, value, cancellationToken);
        }
        return ErrorCode.NotSupported;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (RowsInput is IRowsInputDelete deleteRows)
        {
            return await deleteRows.DeleteAsync(cancellationToken);
        }
        return ErrorCode.NotSupported;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        if (RowsInput is IRowsInputKeys keysInput)
        {
            return keysInput.GetKeyColumns();
        }
        return [];
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        if (RowsInput is IRowsInputKeys keysInput)
        {
            keysInput.SetKeyColumnValue(columnIndex, value, operation);
        }
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        if (RowsInput is IRowsInputKeys keysInput)
        {
            keysInput.UnsetKeyColumnValue(columnIndex, operation);
        }
    }
}
