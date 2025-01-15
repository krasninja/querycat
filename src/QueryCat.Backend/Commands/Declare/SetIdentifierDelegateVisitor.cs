using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetIdentifierDelegateVisitor : CreateDelegateVisitor
{
    private readonly IFuncUnit _funcUnit;

    public SetIdentifierDelegateVisitor(IExecutionThread<ExecutionOptions> thread, ResolveTypesVisitor resolveTypesVisitor, IFuncUnit funcUnit)
        : base(thread, resolveTypesVisitor)
    {
        _funcUnit = funcUnit;
    }

    public override IFuncUnit RunAndReturn(IAstNode node)
    {
        if (node is not IdentifierExpressionNode)
        {
            throw new InvalidOperationException($"Node's type must be '{nameof(IdentifierExpressionNode)}'.");
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
            async ValueTask<VariantValue> Func(IExecutionThread thread, CancellationToken cancellationToken)
            {
                var newValue = await SetValueAsync(thread, node, strategies, context, cancellationToken);
                context.Clear();
                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    private async ValueTask<VariantValue> SetValueAsync(
        IExecutionThread thread,
        IdentifierExpressionNode node,
        SelectStrategyContainer selectStrategyContainer,
        ObjectSelectorContext context,
        CancellationToken cancellationToken)
    {
        var startObject = thread.GetVariable(node.Name);
        var newValue = await _funcUnit.InvokeAsync(thread, cancellationToken);

        // This is expression object.
        context.ExecutionThread = thread;
        // Fills the context.
        await GetObjectBySelectorAsync(thread, context, startObject, selectStrategyContainer, cancellationToken);
        var set = thread.ObjectSelector.SetValue(context, Converter.ConvertValue(newValue, typeof(object)));
        context.ExecutionThread = NullExecutionThread.Instance;
        if (!set)
        {
            thread.ObjectSelector.SetValue(context, Converter.ConvertValue(newValue, typeof(object)));
        }
        // Not an expression - variable.
        if (!set && !node.HasSelectors)
        {
            thread.TopScope.Variables[node.Name] = newValue;
        }

        return newValue;
    }
}
