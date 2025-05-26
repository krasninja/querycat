using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Adds a delay before reading a next record.
/// </summary>
internal sealed class DelayRowsInput : IRowsInputKeys
{
    [SafeFunction]
    [Description("Implements delay before reading the next record.")]
    [FunctionSignature("delay_input(input: object<IRowsInput>, delay_secs: integer := 5): object<IRowsInput>")]
    public static VariantValue DelayInput(IExecutionThread thread)
    {
        var input = thread.Stack[0].AsRequired<IRowsInput>();
        var delaySeconds = (int)(thread.Stack[1].AsInteger ?? 5);
        return VariantValue.CreateFromObject(new DelayRowsInput(input, TimeSpan.FromSeconds(delaySeconds)));
    }

    private readonly IRowsInput _rowsInput;
    private readonly TimeSpan _delay;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsInput.QueryContext;
        set => _rowsInput.QueryContext = value;
    }

    public DelayRowsInput(IRowsInput rowsInput, TimeSpan delay)
    {
        _rowsInput = rowsInput;
        _delay = delay;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return await _rowsInput.ReadNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _rowsInput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _rowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _rowsInput.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            return rowsInputKeys.GetKeyColumns();
        }
        return [];
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            rowsInputKeys.SetKeyColumnValue(columnIndex, value, operation);
        }
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        if (_rowsInput is IRowsInputKeys rowsInputKeys)
        {
            rowsInputKeys.UnsetKeyColumnValue(columnIndex, operation);
        }
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Delay", _rowsInput);
    }
}
