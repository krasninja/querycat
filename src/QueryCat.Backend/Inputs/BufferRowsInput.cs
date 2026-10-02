using System.ComponentModel;
using System.Threading.Channels;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

internal sealed class BufferRowsInput : BufferRowsSource, IRowsInput, IRowsIteratorParent
{
    [SafeFunction]
    [Description("Implements buffer for rows input.")]
    [FunctionSignature("buffer_input(input: object<IRowsInput>, size: integer := 1024): object<IRowsInput>")]
    public static VariantValue BufferInput(IExecutionThread thread)
    {
        var input = thread.Stack[0].AsRequired<IRowsInput>();
        var bufferSize = GetBufferSize(thread.Stack[1]);
        return VariantValue.CreateFromObject(new BufferRowsInput(input, bufferSize));
    }

    private readonly IRowsInput _rowsInput;
    private VariantValue[]? _currentValues;

    /// <inheritdoc />
    protected override bool FlushOnStop => false;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    public BufferRowsInput(IRowsInput rowsInput, int bufferSize) : base(rowsInput, bufferSize)
    {
        _rowsInput = rowsInput;
    }

    /// <inheritdoc />
    protected override async Task RunWorkerAsync(Channel<VariantValue[]> channel, CancellationToken cancellationToken)
    {
        var writer = channel.Writer;
        while (await _rowsInput.ReadNextAsync(cancellationToken))
        {
            var values = new VariantValue[_rowsInput.Columns.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = _rowsInput.ReadValue(i, out var value) == ErrorCode.OK ? value : VariantValue.Null;
            }
            // Waits while the buffer is full.
            await writer.WriteAsync(values, cancellationToken);
        }
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_currentValues == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        value = _currentValues[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        var reader = EnsureWorkerStarted().Reader;
        // WaitToReadAsync rethrows the source exception if the worker failed.
        while (await reader.WaitToReadAsync(cancellationToken))
        {
            if (reader.TryRead(out _currentValues))
            {
                return true;
            }
        }

        _currentValues = null;
        return false;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _currentValues = null;
        await base.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _currentValues = null;
        await base.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _rowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        ThrowIfWorkerStarted();
        _rowsInput.SetKeyColumnValue(columnIndex, value, operation);
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        ThrowIfWorkerStarted();
        _rowsInput.UnsetKeyColumnValue(columnIndex, operation);
    }

    // Buffered rows were read with the previous key values, and the worker may be inside the source right now.
    // The planner always calls ResetAsync (which stops the worker) before changing keys.
    private void ThrowIfWorkerStarted()
    {
        if (IsWorkerStarted)
        {
            throw new QueryCatException(Resources.Errors.BufferKeysAfterRead);
        }
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsInput;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Buffer", _rowsInput);
    }
}
