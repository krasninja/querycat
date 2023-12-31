using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private IFuncUnit Misc_CreateDelegate(IAstNode node, SelectCommandContext? context = null)
    {
        if (context != null)
        {
            return new SelectCreateDelegateVisitor(ExecutionThread, context).RunAndReturn(node);
        }
        else
        {
            return new CreateDelegateVisitor(ExecutionThread).RunAndReturn(node);
        }
    }

    private IEnumerable<IFuncUnit> Misc_CreateDelegate(IEnumerable<IAstNode> nodes, SelectCommandContext? context = null)
    {
        var visitor = context != null
            ? new SelectCreateDelegateVisitor(ExecutionThread, context)
            : new CreateDelegateVisitor(ExecutionThread);
        return nodes.Select(n => visitor.RunAndReturn(n));
    }

    private void Misc_Transform(SelectQueryNode node)
    {
        new TransformQueryAstVisitor().Run(node);
    }
}
