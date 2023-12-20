using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var setNode = (SetNode)node.RootNode;
        var scope = executionThread.TopScope;

        var valueHandler = new StatementsVisitor(executionThread).RunAndReturn(setNode.ValueNode);

        return new FuncCommandHandler(() =>
        {
            var value = valueHandler.Invoke();
            scope.Variables[setNode.Name] = value;
            return VariantValue.Null;
        });
    }
}
