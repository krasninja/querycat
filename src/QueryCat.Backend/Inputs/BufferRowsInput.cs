using System.ComponentModel;
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
        var bufferSize = (int)(thread.Stack[1].AsInteger ?? 1024);
        return VariantValue.CreateFromObject(new BufferRowsInput(input, bufferSize));
    }

    private readonly IRowsInput _rowsInput;
    private Row? _currentRow;

    /// <summary>
    /// The semaphore is used to sync queue read and write in case of empty queue.
    /// </summary>
    private readonly SemaphoreSlim _writeSemaphore = new(1);

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    /// <inheritdoc />
    public BufferRowsInput(IRowsInput rowsInput, int bufferSize) : base(rowsInput, bufferSize)
    {
        _rowsInput = rowsInput;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> CallbackAsync(CancellationToken cancellationToken)
    {
        await QueueCountSemaphore.WaitAsync(cancellationToken);

        var result = await _rowsInput.ReadNextAsync(cancellationToken);
        if (!result)
        {
            return false;
        }
        var row = new Row(_rowsInput.Columns);
        for (var i = 0; i < _rowsInput.Columns.Length; i++)
        {
            var errorCode = _rowsInput.ReadValue(i, out var value);
            row[i] = errorCode == ErrorCode.OK ? value : VariantValue.Null;
        }

        RowsQueue.Enqueue(row);
        return true;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_currentRow == null)
        {
            value = VariantValue.Null;
            return ErrorCode.NoData;
        }

        value = _currentRow[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        StartThread();
        do
        {
            // Otherwise wait for complete write and try to dequeue again.
            if (RowsQueue.TryDequeue(out _currentRow)
                && _currentRow is not null)
            {
                QueueCountSemaphore.Release();
                return true;
            }
            await Task.Yield();
        }
        while (!(EndOfData && RowsQueue.IsEmpty));

        return false;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await base.ResetAsync(cancellationToken);
        await _writeSemaphore.WaitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _rowsInput.GetKeyColumns();

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        _rowsInput.SetKeyColumnValue(columnIndex, value, operation);
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        _rowsInput.UnsetKeyColumnValue(columnIndex, operation);
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _writeSemaphore.Dispose();
        base.Dispose(disposing);
    }
}
