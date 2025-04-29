using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class ExecutionFlowFuncUnit : IExecutionFlowFuncUnit, IFuncUnit
{
    /// <inheritdoc />
    public ExecutionJump Jump { get; }

    /// <inheritdoc />
    public DataType OutputType => DataType.Void;

    public ExecutionFlowFuncUnit(ExecutionJump jump)
    {
        Jump = jump;
    }

    /// <inheritdoc />
    public ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(VariantValue.Null);
}
