using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Delete;

internal sealed class DeleteCommandHandler : IFuncUnit
{
    private readonly SelectCommandContext _selectCommandContext;
    private readonly IRowsInputDelete _rowsInputDelete;

    /// <inheritdoc />
    public DataType OutputType => DataType.Integer;

    public DeleteCommandHandler(SelectCommandContext selectCommandContext, IRowsInputDelete rowsInputDelete)
    {
        _selectCommandContext = selectCommandContext;
        _rowsInputDelete = rowsInputDelete;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var deleteCount = 0;
        while (await _selectCommandContext.CurrentIterator.MoveNextAsync(cancellationToken))
        {
            if (await _rowsInputDelete.DeleteAsync(cancellationToken) == ErrorCode.OK)
            {
                deleteCount++;
            }
        }

        return new VariantValue(deleteCount);
    }
}
