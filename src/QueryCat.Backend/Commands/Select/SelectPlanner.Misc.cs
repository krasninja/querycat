using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select.Visitors;

namespace QueryCat.Backend.Commands.Select;

internal sealed partial class SelectPlanner
{
    private async Task<IFuncUnit> Misc_CreateDelegateAsync(
        IAstNode node,
        SelectCommandContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (context != null)
        {
            return await new SelectCreateDelegateVisitor(ExecutionThread, context)
                .RunAndReturnAsync(node, cancellationToken);
        }
        else
        {
            return await new CreateDelegateVisitor(ExecutionThread, _resolveTypesVisitor)
                .RunAndReturnAsync(node, cancellationToken);
        }
    }

    private async Task<IFuncUnit[]> Misc_CreateDelegateAsync(
        IEnumerable<IAstNode> nodes,
        SelectCommandContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var visitor = context != null
            ? new SelectCreateDelegateVisitor(ExecutionThread, context)
            : new CreateDelegateVisitor(ExecutionThread, _resolveTypesVisitor);
        var list = new List<IFuncUnit>();
        foreach (var node in nodes)
        {
            list.Add(await visitor.RunAndReturnAsync(node, cancellationToken));
        }
        return list.ToArray();
    }

    private async Task Misc_TransformAsync(SelectQueryNode node, CancellationToken cancellationToken)
    {
        await new TransformQueryAstVisitor().RunAsync(node, cancellationToken);
    }
}
