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
            var context = new ObjectSelectorContext(ExecutionThread);
            VariantValue Func()
            {
                var newValue = SetValue(node, context);
                context.Clear();
                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    private VariantValue SetValue(IdentifierExpressionNode node, ObjectSelectorContext context)
    {
        var startObject = ExecutionThread.GetVariable(node.Name);
        var newValue = _funcUnit.Invoke();

        // This is expression object.
        if (GetObjectBySelector(context, startObject, node, out _))
        {
            ExecutionThread.ObjectSelector.SetValue(context, Converter.ConvertValue(newValue, typeof(object)));
        }
        // Not an expression - variable.
        else if (!node.HasSelectors)
        {
            ExecutionThread.TopScope.Variables[node.Name] = newValue;
        }

        return newValue;
    }
}
