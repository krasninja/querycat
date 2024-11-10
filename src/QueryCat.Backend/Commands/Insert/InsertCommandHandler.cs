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
    public VariantValue Invoke(IExecutionThread thread)
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
