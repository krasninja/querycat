using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// SELECT command.
/// </summary>
internal sealed class SelectCommand : ICommand
{
    private readonly ResolveTypesVisitor _resolveTypesVisitor;

    public SelectCommand(ResolveTypesVisitor resolveTypesVisitor)
    {
        _resolveTypesVisitor = resolveTypesVisitor;
    }

    /// <inheritdoc />
    public IFuncUnit CreateHandler(IExecutionThread<ExecutionOptions> executionThread, StatementNode node)
    {
        var selectQueryNode = (SelectQueryNode)node.RootNode;

        // Iterate by select node in pre-order way and create correspond command context.
        new SelectPlanner(executionThread, _resolveTypesVisitor).CreateIterator(selectQueryNode);
        var context = selectQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        IFuncUnit handler = new SelectCommandHandler(context);
        return handler;
    }
}
