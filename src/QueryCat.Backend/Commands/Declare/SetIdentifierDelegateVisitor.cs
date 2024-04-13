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
        var scope = ExecutionThread.TopScope;

        if (scope.TryGet(node.Name, out _))
        {
            var context = new ObjectSelectorContext();
            VariantValue Func()
            {
                var startObject = scope.Get(node.Name);
                var newValue = _funcUnit.Invoke();
                if (GetObjectBySelector(context, startObject, node, out _))
                {
                    var lastContext = context.SelectStack.Pop();
                    if (lastContext.PropertyInfo != null)
                    {
                        ExecutionThread.ObjectSelector.SetValue(
                            obj: context.SelectStack.Peek().Object,
                            newValue: Converter.ConvertValue(newValue, typeof(object)),
                            propertyInfo: lastContext.PropertyInfo,
                            indexes: []);
                    }
                }
                else if (!node.HasSelectors)
                {
                    scope.Variables[node.Name] = newValue;
                }

                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }
}
