using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Declare;

internal sealed class SetIdentifierDelegateVisitor : CreateDelegateVisitor
{
    private readonly IFuncUnit _funcUnit;

    public SetIdentifierDelegateVisitor(
        IExecutionThread<ExecutionOptions> thread,
        ResolveTypesVisitor resolveTypesVisitor,
        IFuncUnit funcUnit)
        : base(thread, resolveTypesVisitor)
    {
        _funcUnit = funcUnit;
    }

    /// <inheritdoc />
    public override ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
    {
        if (node is not IdentifierExpressionNode)
        {
            throw new InvalidOperationException($"Node's type must be '{nameof(IdentifierExpressionNode)}'.");
        }
        return base.RunAndReturnAsync(node, cancellationToken);
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
        var set = await thread.ObjectSelector.SetValueAsync(context, Converter.ConvertValue(newValue, typeof(object)), cancellationToken);
        context.ExecutionThread = NullExecutionThread.Instance;
        if (!set)
        {
            await thread.ObjectSelector.SetValueAsync(context, Converter.ConvertValue(newValue, typeof(object)), cancellationToken);
        }
        // Not an expression - variable.
        if (!set && !node.HasSelectors)
        {
            thread.TopScope.Variables[node.Name] = newValue;
        }

        return newValue;
    }
}
