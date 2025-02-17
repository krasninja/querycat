using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.If;

internal sealed class IfConditionCommand : ICommand
{
    private sealed record ConditionItem(IFuncUnit Condition, IFuncUnit Block);

    /// <inheritdoc />
    public async Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var ifConditionNode = (IfConditionNode)node.RootNode;
        var statementVisitor = new StatementsVisitor(executionThread);
        var delegateVisitor = new CreateDelegateVisitor(executionThread);

        var conditionsFuncsArray = new ConditionItem[ifConditionNode.ConditionsList.Count];
        for (var i = 0; i < ifConditionNode.ConditionsList.Count; i++)
        {
            var condition = ifConditionNode.ConditionsList[i];
            var conditionFunc = await delegateVisitor.RunAndReturnAsync(condition.ConditionNode, cancellationToken);
            var blockFunc = await statementVisitor.RunAndReturnAsync(condition.BlockExpressionNode, cancellationToken);
            conditionsFuncsArray[i] = new ConditionItem(conditionFunc, blockFunc);
        }

        var elseFunc = ifConditionNode.ElseNode != null
            ? await statementVisitor.RunAndReturnAsync(ifConditionNode.ElseNode, cancellationToken)
            : null;

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            foreach (var conditionFunc in conditionsFuncsArray)
            {
                if ((await conditionFunc.Condition.InvokeAsync(thread, ct)).AsBoolean)
                {
                    return await conditionFunc.Block.InvokeAsync(thread, ct);
                }
            }

            if (elseFunc != null)
            {
                return await elseFunc.InvokeAsync(thread, ct);
            }

            return VariantValue.Null;
        }

        IFuncUnit handler = new FuncUnitDelegate(Func, conditionsFuncsArray[0].Block.OutputType);
        return handler;
    }
}
