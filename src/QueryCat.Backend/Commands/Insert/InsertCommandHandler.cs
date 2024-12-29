using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Insert;

internal sealed class InsertCommandHandler : IFuncUnit
{
    private readonly IRowsIterator _rowsInput;
    private readonly IRowsOutput _rowsOutput;

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    public InsertCommandHandler(IRowsIterator rowsInput, IRowsOutput rowsOutput)
    {
        _rowsInput = rowsInput;
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var insertCount = 0;
        _rowsOutput.QueryContext = new RowsOutputQueryContext(_rowsInput.Columns);
        await _rowsOutput.OpenAsync(cancellationToken);
        try
        {
            while (await _rowsInput.MoveNextAsync(cancellationToken))
            {
                await _rowsOutput.WriteValuesAsync(_rowsInput.Current.Values, cancellationToken);
                insertCount++;
            }
        }
        finally
        {
            await _rowsOutput.CloseAsync(cancellationToken);
        }

        return new VariantValue(insertCount);
    }
}
