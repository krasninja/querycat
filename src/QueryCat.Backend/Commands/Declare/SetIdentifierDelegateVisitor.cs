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

        if (scope.Contains(node.Name))
        {
            var context = new ObjectSelectorContext();
            VariantValue Func()
            {
                var newValue = SetValue(node, scope, context);
                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    private VariantValue SetValue(IdentifierExpressionNode node, IExecutionScope scope, ObjectSelectorContext context)
    {
        var startObject = scope.Get(node.Name);
        var newValue = _funcUnit.Invoke();

        // This is expression object.
        if (GetObjectBySelector(context, startObject, node, out _))
        {
            var lastContext = context.SelectStack.Pop();
            var lastSelector = node.SelectorNodes.LastOrDefault();

            // The expression object ends with property (obj.Address.City).
            if (lastSelector is IdentifierPropertySelectorNode)
            {
                ExecutionThread.ObjectSelector.SetValue(
                    selectInfo: lastContext,
                    newValue: Converter.ConvertValue(newValue, typeof(object)),
                    indexes: []);
            }
            // The expression object ends with index (obj.Phone[3]).
            else if (lastSelector is IdentifierIndexSelectorNode indexSelectorNode)
            {
                var indexObjects = GetObjectIndexesSelector(indexSelectorNode);
                ExecutionThread.ObjectSelector.SetValue(
                    selectInfo: lastContext,
                    newValue: Converter.ConvertValue(newValue, typeof(object)),
                    indexes: indexObjects);
            }
        }
        // Not an expression - variable.
        else if (!node.HasSelectors)
        {
            scope.Variables[node.Name] = newValue;
        }

        return newValue;
    }
}
