using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectCommandHandler : IFuncUnit, IDisposable
{
    public SelectCommandContext SelectCommandContext { get; }

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    public SelectCommandHandler(SelectCommandContext selectCommandContext)
    {
        SelectCommandContext = selectCommandContext;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        await ResetVariablesBoundRowsInputsAsync(cancellationToken);
        return VariantValue.CreateFromObject(SelectCommandContext.CurrentIterator);
    }

    private async ValueTask ResetVariablesBoundRowsInputsAsync(CancellationToken cancellationToken)
    {
        foreach (var inputQueryContext in SelectCommandContext.Inputs)
        {
            if (inputQueryContext.IsVariableBound)
            {
                await inputQueryContext.RowsInput.ResetAsync(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SelectCommandContext.Dispose();
    }
}
