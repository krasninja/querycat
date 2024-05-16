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

        if (ExecutionThread.ContainsVariable(node.Name, scope))
        {
            var context = new ObjectSelectorContext();
            VariantValue Func()
            {
                var newValue = SetValue(node, scope, context);
                context.Clear();
                return newValue;
            }
            NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, node.GetDataType());
            return;
        }

        throw new CannotFindIdentifierException(node.Name);
    }

    private VariantValue SetValue(IdentifierExpressionNode node, IExecutionScope scope, ObjectSelectorContext context)
    {
        var startObject = ExecutionThread.GetVariable(node.Name, scope);
        var newValue = _funcUnit.Invoke();

        // This is expression object.
        if (GetObjectBySelector(context, startObject, node, out _))
        {
            var lastToken = context.Pop();
            var owner = context.PreviousResult;
            var lastSelector = node.SelectorNodes.LastOrDefault();

            if (owner == null)
            {
                throw new InvalidOperationException("Cannot set value to null object.");
            }

            // The expression object ends with property (obj.Address.City).
            if (lastSelector is IdentifierPropertySelectorNode)
            {
                ExecutionThread.ObjectSelector.SetValue(
                    token: lastToken,
                    owner: owner,
                    newValue: Converter.ConvertValue(newValue, typeof(object)),
                    indexes: []);
            }
            // The expression object ends with index (obj.Phone[3]).
            else if (lastSelector is IdentifierIndexSelectorNode indexSelectorNode)
            {
                var indexObjects = GetObjectIndexesSelector(indexSelectorNode);
                ExecutionThread.ObjectSelector.SetValue(
                    token: lastToken,
                    owner: owner,
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
