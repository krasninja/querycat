using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Insert;

internal sealed class InsertCommandHandler : CommandHandler
{
    private readonly ExecutionThread _executionThread;
    private readonly IRowsIterator _rowsInput;
    private readonly IRowsOutput _rowsOutput;

    public InsertCommandHandler(ExecutionThread executionThread, IRowsIterator rowsInput, IRowsOutput rowsOutput)
    {
        _executionThread = executionThread;
        _rowsInput = rowsInput;
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        var insertCount = 0;
        _rowsOutput.Open();
        try
        {
            _rowsOutput.QueryContext = new RowsOutputQueryContext(_rowsInput.Columns);
            while (_rowsInput.MoveNext())
            {
                _rowsOutput.WriteValues(_rowsInput.Current.Values);
                insertCount++;
            }
        }
        finally
        {
            _rowsOutput.Close();
        }

        return new VariantValue(insertCount);
    }
}
