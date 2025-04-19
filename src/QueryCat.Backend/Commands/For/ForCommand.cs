using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.For;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.For;

internal sealed class ForCommand : ICommand
{
    /// <inheritdoc />
    public async Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var forNode = (ForNode)node.RootNode;

        var query = await new CreateDelegateVisitor(executionThread)
            .RunAndReturnAsync(forNode.QueryExpression, cancellationToken);
        var queryValue = await query.InvokeAsync(executionThread, cancellationToken);
        var iterator = RowsIteratorConverter.Convert(queryValue);

        var scope = executionThread.PushScope();
        scope.Variables[forNode.TargetVariableName] = VariantValue.CreateFromObject(new Row(iterator.Current));
        var loopBody = await new CreateDelegateVisitor(executionThread)
            .RunAndReturnAsync(forNode.ProgramBodyNode, cancellationToken);
        executionThread.PopScope();
        return new ForCommandHandler(forNode.TargetVariableName, iterator, loopBody);
    }
}
