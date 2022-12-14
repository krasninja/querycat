using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Declare;

internal class SetCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var setNode = (SetNode)node.RootNode;
        var varIndex = executionThread.RootScope.GetVariableIndex(setNode.Name, out var scope);
        if (varIndex < 0 || scope == null)
        {
            throw new CannotFindIdentifierException(setNode.Name);
        }

        var valueHandler = new StatementsVisitor(executionThread).RunAndReturn(setNode.ValueNode);

        return new FuncCommandHandler(() =>
        {
            var value = valueHandler.Invoke();
            scope.SetVariable(varIndex, value);
            return VariantValue.Null;
        });
    }
}
