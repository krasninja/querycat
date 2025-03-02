using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

internal sealed class BufferRowsInput : BufferRowsSource, IRowsInput
{
    [SafeFunction]
    [Description("Implements buffer for rows input.")]
    [FunctionSignature("buffer_input(input: object<IRowsInput>, size: integer := 1024): object<IRowsInput>")]
    public static VariantValue BufferInput(IExecutionThread thread)
    {
        var input = thread.Stack[0].AsRequired<IRowsInput>();
        var bufferSize = (int)(thread.Stack[1].AsInteger ?? 24);
        return VariantValue.CreateFromObject(new BufferRowsInput(input, bufferSize));
    }

    private readonly IRowsInput _rowsInput;
    private Row? _currentRow;

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
    protected override async ValueTask<Row?> CallbackAsync(CancellationToken cancellationToken)
    {
        var result = await _rowsInput.ReadNextAsync(cancellationToken);
        if (!result)
        {
            return null;
        }
        var row = new Row(_rowsInput.Columns);
        for (var i = 0; i < _rowsInput.Columns.Length; i++)
        {
            var errorCode = _rowsInput.ReadValue(i, out var value);
            row[i] = errorCode == ErrorCode.OK ? value : VariantValue.Null;
        }
        return row;
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
        // Try to get new row - fast path.
        if (RowsQueue.TryDequeue(out _currentRow)
            && _currentRow is not null)
        {
            QueueCountSemaphore.Release();
            return true;
        }

        while (!EndOfData)
        {
            try
            {
                // Otherwise wait for complete write and try to dequeue again.
                await WriteSemaphore.WaitAsync(cancellationToken);
                if (RowsQueue.TryDequeue(out _currentRow)
                    && _currentRow is not null)
                {
                    QueueCountSemaphore.Release();
                    return true;
                }
            }
            finally
            {
                WriteSemaphore.Release();
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Buffer", _rowsInput);
    }
}
