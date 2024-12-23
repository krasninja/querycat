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
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        ResetVariablesBoundRowsInputs();
        return ValueTask.FromResult(VariantValue.CreateFromObject(SelectCommandContext.CurrentIterator));
    }

    private void ResetVariablesBoundRowsInputs()
    {
        foreach (var inputQueryContext in SelectCommandContext.Inputs)
        {
            if (inputQueryContext.IsVariableBound)
            {
                inputQueryContext.RowsInput.Reset();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SelectCommandContext.Dispose();
    }
}
