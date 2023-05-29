using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Declare;

internal class DeclareCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var declareNode = (DeclareNode)node.RootNode;
        var scope = executionThread.TopScope;

        CommandHandler valueHandler = FuncCommandHandler.NullHandler;
        if (declareNode.ValueNode != null)
        {
            valueHandler = executionThread.StatementsVisitor.RunAndReturn(declareNode.ValueNode);
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
            scope.DefineVariable(declareNode.Name, declareNode.Type, value);
            return VariantValue.Null;
        });
    }
}
