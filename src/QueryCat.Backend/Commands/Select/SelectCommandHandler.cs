using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectCommandHandler : IFuncUnit, IAsyncDisposable
{
    public SelectCommandContext SelectCommandContext { get; }

    /// <inheritdoc />
    public DataType OutputType { get; private set; } = DataType.Null;

    public SelectCommandHandler(SelectCommandContext selectCommandContext)
    {
        SelectCommandContext = selectCommandContext;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        await ResetVariablesBoundRowsInputsAsync(cancellationToken);
        if (SelectCommandContext.CurrentIterator.Columns.Length == 1)
        {
            OutputType = SelectCommandContext.CurrentIterator.Columns[0].DataType;
        }
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
    public ValueTask DisposeAsync() => SelectCommandContext.CloseAsync();
}
