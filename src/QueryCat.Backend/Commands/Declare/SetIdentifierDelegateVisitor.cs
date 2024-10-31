using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetIdentifierDelegateVisitor : CreateDelegateVisitor
{
    private readonly IFuncUnit _funcUnit;

    public SetIdentifierDelegateVisitor(IExecutionThread<ExecutionOptions> thread, IFuncUnit funcUnit) : base(thread)
    {
        _funcUnit = funcUnit;
    }

    public SetIdentifierDelegateVisitor(IExecutionThread<ExecutionOptions> thread, ResolveTypesVisitor resolveTypesVisitor, IFuncUnit funcUnit)
        : base(thread, resolveTypesVisitor)
    {
        _funcUnit = funcUnit;
    }

    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        if (node is not IdentifierExpressionNode)
        {
            throw new InvalidOperationException($"Node's type must be {nameof(IdentifierExpressionNode)}.");
        }
        return base.RunAndReturn(node);
    }

    public override void Visit(IdentifierExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        if (ExecutionThread.ContainsVariable(node.Name))
        {
            var context = new ObjectSelectorContext();
            var strategies = GetObjectSelectStrategies(node, NodeIdFuncMap);
            VariantValue Func(IExecutionThread thread)
            {
                var newValue = SetValue(thread, node, strategies, context);
                context.Clear();
                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    private VariantValue SetValue(
        IExecutionThread thread,
        IdentifierExpressionNode node,
        SelectStrategyContainer selectStrategyContainer,
        ObjectSelectorContext context)
    {
        var startObject = thread.GetVariable(node.Name);
        var newValue = _funcUnit.Invoke(thread);

        // This is expression object.
        if (GetObjectBySelector(thread, context, startObject, selectStrategyContainer, out _))
        {
            context.ExecutionThread = thread;
            thread.ObjectSelector.SetValue(context, Converter.ConvertValue(newValue, typeof(object)));
            context.ExecutionThread = NullExecutionThread.Instance;
        }
        // Not an expression - variable.
        else if (!node.HasSelectors)
        {
            thread.TopScope.Variables[node.Name] = newValue;
        }

        return newValue;
    }
}
