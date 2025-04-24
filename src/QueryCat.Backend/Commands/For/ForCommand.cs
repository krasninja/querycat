using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.For;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.For;

internal sealed class ForCommand : ICommand
{
    private readonly StatementsVisitor _statementsVisitor;

    public ForCommand(StatementsVisitor statementsVisitor)
    {
        _statementsVisitor = statementsVisitor;
    }

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

        return new ForCommandHandler(_statementsVisitor, forNode.ProgramBodyNode, forNode.TargetVariableName, iterator);
    }
}
