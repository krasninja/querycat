using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// The input source that will be re-created on reset. It is used for such input source
/// types that have references to another input source.
/// </summary>
internal sealed class VaryingRowsInput : IRowsInputDelete, IRowsInputUpdate, IRowsIteratorParent
{
    private readonly IRowsInput _initialRowsInput;
    private readonly QueryContext _queryContext;
    private readonly IExecutionThread<ExecutionOptions> _thread;
    private IRowsInput? _currentInput;
    private int[] _sourceColumnsMapping = [];

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
    public Column[] Columns => _initialRowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _initialRowsInput.UniqueKey;

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

        InitializeSourceColumnsMapping(initial: true);
    }

    private void InitializeSourceColumnsMapping(bool initial = false)
    {
        if (Columns.Length != _sourceColumnsMapping.Length)
        {
            _sourceColumnsMapping = new int[Columns.Length];
        }

        if (initial)
        {
            for (var i = 0; i < _sourceColumnsMapping.Length; i++)
            {
                _sourceColumnsMapping[i] = i;
            }
        }
        else
        {
            if (_currentInput != null)
            {
                for (var i = 0; i < _sourceColumnsMapping.Length; i++)
                {
                    var columnIndex = _currentInput.GetColumnIndexByName(Columns[i].Name);
                    _sourceColumnsMapping[i] = columnIndex;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await RowsInput.OpenAsync(cancellationToken);

        if (Columns.Length != _sourceColumnsMapping.Length)
        {
            _sourceColumnsMapping = new int[Columns.Length];
        }
        for (var i = 0; i < _sourceColumnsMapping.Length; i++)
        {
            _sourceColumnsMapping[i] = i;
        }
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => RowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _isEndOfData = false;
        _store.Clear();
        _currentInput = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_currentInput == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        var index = _sourceColumnsMapping[columnIndex];
        if (index < 0)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        return _currentInput.ReadValue(index, out value);
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
            var newInputContext = await _rowsInputFactory.CreateRowsInputAsync(
                callResult.Value, _thread, true, cancellationToken);
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

            InitializeSourceColumnsMapping();
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
    public IReadOnlyList<KeyColumn> GetKeyColumns() => RowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        RowsInput.SetKeyColumnValue(columnIndex, value, operation);
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        RowsInput.UnsetKeyColumnValue(columnIndex, operation);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return RowsInput;
    }
}
