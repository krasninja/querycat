using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Open;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Open;

/// <summary>
/// Handles the OPEN statement — evaluates the source expression and returns an opened IRowsInput.
/// </summary>
internal sealed class OpenCommand : ICommand
{
    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var openNode = (OpenNode)node.RootNode;

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            var localThread = (IExecutionThread<ExecutionOptions>)thread;
            var delegateVisitor = new CreateDelegateVisitor(localThread);
            var rowsInputFactory = new RowsInputFactory(
                new SelectCommandContext(new SelectOpenNode(openNode))
            );
            var sourceDelegate = await delegateVisitor.RunAndReturnAsync(openNode.Expression, ct);
            var source = await sourceDelegate.InvokeAsync(thread, ct);
            var context = await rowsInputFactory.CreateRowsInputAsync(
                source,
                localThread,
                resolveStringAsSource: true,
                cancellationToken: ct);
            if (context == null)
            {
                return VariantValue.Null;
            }

            await context.RowsInput.OpenAsync(ct);

            return VariantValue.CreateFromObject(context.RowsInput);
        }

        IFuncUnit handler = new FuncUnitDelegate(Func, DataType.Object);
        return Task.FromResult(handler);
    }
}
