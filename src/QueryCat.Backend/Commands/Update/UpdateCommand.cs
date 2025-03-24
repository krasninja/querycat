using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.Update;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Select.Visitors;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Commands.Update;

internal sealed class UpdateCommand : ICommand
{
    /// <inheritdoc />
    public async Task<IFuncUnit> CreateHandlerAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        StatementNode node,
        CancellationToken cancellationToken = default)
    {
        if (executionThread.Options.SafeMode)
        {
            throw new SafeModeException();
        }

        var insertNode = (UpdateNode)node.RootNode;

        // Format SELECT statement and use it for further iterations.
        var selectNode = new SelectQuerySpecificationNode(
            new SelectColumnsListNode(insertNode.SetNodes.Select(n => new SelectColumnsSublistExpressionNode(n.SetTargetNode))
        ));
        selectNode.TableExpressionNode =
            new SelectTableNode(
                new SelectTableReferenceListNode(insertNode.TargetExpressionNode));
        selectNode.TableExpressionNode.SearchConditionNode = insertNode.SearchConditionNode;
        await new SelectPlanner(executionThread, operationIntentionType: SelectPlanner.OperationIntentionType.Update)
            .CreateIteratorAsync(selectNode, cancellationToken: cancellationToken);
        var context = selectNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey);

        // Evaluate setters.
        if (context.RowsInputIterator?.RowsInput is not IRowsInputUpdate rowsInput)
        {
            throw new QueryCatException(Resources.Errors.RowsInputNotUpdatable);
        }
        var setters = new List<UpdateSetter>();
        var createDelegateVisitor = new SelectCreateDelegateVisitor(executionThread, context);
        foreach (var setNode in insertNode.SetNodes)
        {
            var columnIndex = rowsInput.GetColumnIndexByName(setNode.SetTargetNode.TableFieldName,
                setNode.SetTargetNode.TableSourceName);
            if (columnIndex < 0)
            {
                throw new QueryCatException(
                    string.Format(Resources.Errors.CannotFindColumn, setNode.SetTargetNode.FullName));
            }
            var func = await createDelegateVisitor.RunAndReturnAsync(setNode.SetSourceNode, cancellationToken);
            setters.Add(new UpdateSetter(columnIndex, func));
        }

        IFuncUnit handler = new UpdateCommandHandler(context, setters.ToArray());
        return handler;
    }
}
