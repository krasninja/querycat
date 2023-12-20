using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.FunctionsManager;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class DeclareCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var declareNode = (DeclareNode)node.RootNode;
        var scope = executionThread.TopScope;

        CommandHandler valueHandler = FuncCommandHandler.NullHandler;
        if (declareNode.ValueNode != null)
        {
            valueHandler = new StatementsVisitor(executionThread).RunAndReturn(declareNode.ValueNode);
            // There is a special case for SELECT command. We prefer assign first value instead of iterator object.
            if (valueHandler is SelectCommandHandler selectCommandHandler)
            {
                valueHandler = new FuncUnitCommandHandler(
                    new FuncUnitRowsIteratorScalar(selectCommandHandler.SelectCommandContext.CurrentIterator));
            }
        }

        return new FuncCommandHandler(() =>
        {
            var value = valueHandler.Invoke();
            value = value.Cast(declareNode.Type);
            scope.Variables[declareNode.Name] = value;
            return VariantValue.Null;
        });
    }
}
