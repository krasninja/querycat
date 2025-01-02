using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class DeclareCommand : ICommand
{
    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(IExecutionThread<ExecutionOptions> executionThread, StatementNode node, CancellationToken cancellationToken = default)
    {
        var declareNode = (DeclareNode)node.RootNode;
        var scope = executionThread.TopScope;

        IFuncUnit valueHandler = FuncCommandHandler.NullHandler;
        if (declareNode.ValueNode != null)
        {
            valueHandler = new StatementsVisitor(executionThread).RunAndReturn(declareNode.ValueNode);
            // There is a special case for SELECT command. We prefer assign first value instead of iterator object.
            if (valueHandler is SelectCommandHandler selectCommandHandler
                && selectCommandHandler.SelectCommandContext.IsSingleValue)
            {
                valueHandler = new FuncUnitRowsIteratorScalar(selectCommandHandler.SelectCommandContext.CurrentIterator);
            }
        }

        IFuncUnit handler = new FuncCommandHandler(async (thread, ct) =>
        {
            var value = await valueHandler.InvokeAsync(thread, ct);
            value = value.Cast(declareNode.Type);
            scope.Variables[declareNode.Name] = value;
            return VariantValue.Null;
        });
        return Task.FromResult(handler);
    }
}
