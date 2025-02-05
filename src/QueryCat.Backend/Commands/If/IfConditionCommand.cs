using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.If;

internal sealed class IfConditionCommand : ICommand
{
    /// <inheritdoc />
    public Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        var ifConditionNode = (IfConditionNode)node.RootNode;
        var statementVisitor = new StatementsVisitor(executionThread);
        var delegateVisitor = new CreateDelegateVisitor(executionThread);

        var conditionsFuncs = ifConditionNode.ConditionsList.Select(cl =>
            new
            {
                Condition = delegateVisitor.RunAndReturn(cl.ConditionNode),
                Block = statementVisitor.RunAndReturn(cl.BlockExpressionNode),
            })
            .ToArray();

        var elseFunc = ifConditionNode.ElseNode != null ? statementVisitor.RunAndReturn(ifConditionNode.ElseNode) : null;

        async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken ct)
        {
            foreach (var conditionFunc in conditionsFuncs)
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

        IFuncUnit handler = new FuncUnitDelegate(Func, conditionsFuncs[0].Block.OutputType);
        return Task.FromResult(handler);
    }
}
