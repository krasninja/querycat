using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Declare;

internal class DeclareCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var declareNode = (DeclareNode)node.RootNode;
        var scope = executionThread.RootScope;

        var valueHandler = declareNode.ValueNode != null
            ? new StatementsVisitor(executionThread).RunAndReturn(declareNode.ValueNode)
            : FuncCommandHandler.NullHandler;

        return new FuncCommandHandler(() =>
        {
            var value = valueHandler.Invoke();
            scope.DefineVariable(declareNode.Name, declareNode.Type, value);
            return VariantValue.Null;
        });
    }
}
