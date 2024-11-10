using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.If;

internal sealed class IfConditionCommand : ICommand
{
    /// <inheritdoc />
    public IFuncUnit CreateHandler(IExecutionThread<ExecutionOptions> executionThread, StatementNode node)
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

        VariantValue Func(IExecutionThread thread)
        {
            foreach (var conditionFunc in conditionsFuncs)
            {
                if (conditionFunc.Condition.Invoke(thread).AsBoolean)
                {
                    return conditionFunc.Block.Invoke(thread);
                }
            }

            if (elseFunc != null)
            {
                return elseFunc.Invoke(thread);
            }

            return VariantValue.Null;
        }

        var handler = new FuncUnitDelegate(Func, conditionsFuncs[0].Block.OutputType);
        return handler;
    }
}
