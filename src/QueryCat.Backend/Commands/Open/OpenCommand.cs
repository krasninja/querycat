using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Open;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Open;

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
            var delegateVisitor = new CreateDelegateVisitor(executionThread);
            var rowsInputFactory = new RowsInputFactory(
                new SelectCommandContext(new SelectOpenNode(openNode))
            );
            var sourceDelegate = await delegateVisitor.RunAndReturnAsync(openNode.Expression, ct);
            var source = await sourceDelegate.InvokeAsync(executionThread, ct);
            var context = await rowsInputFactory.CreateRowsInputAsync(
                source,
                executionThread,
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
