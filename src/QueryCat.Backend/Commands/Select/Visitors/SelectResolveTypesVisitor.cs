using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly SelectCommandContext _context;

    /// <inheritdoc />
    public SelectResolveTypesVisitor(IExecutionThread<ExecutionOptions> executionThread, SelectCommandContext context) :
        base(executionThread)
    {
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryCombineNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectTableJoinedOnNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectTableJoinedUsingNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        if (VisitIdentifierNode(node, node.TableFieldName, node.TableSourceName))
        {
            return ValueTask.CompletedTask;
        }
        return base.VisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectExistsExpressionNode node, CancellationToken cancellationToken)
    {
        node.Type = DataType.Boolean;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        node.Type = node.ExpressionNode.Type;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistWindowNode node, CancellationToken cancellationToken)
    {
        node.Type = node.AggregateFunctionNode.Type;
        return ValueTask.CompletedTask;
    }

    private bool VisitIdentifierNode(AstNode node, string name, string source)
    {
        if (!_context.TryGetInputSourceByName(name, source, out var result)
            || result == null)
        {
            return false;
        }

        node.SetAttribute(AstAttributeKeys.InputColumnKey, result.Input.Columns[result.ColumnIndex]);
        node.Type = result.Input.Columns[result.ColumnIndex].DataType;
        return true;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOrderBySpecificationNode node, CancellationToken cancellationToken)
    {
        node.Type = node.ExpressionNode.Type;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        VisitSelectQueryNode(node);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        VisitSelectQueryNode(node);
        return ValueTask.CompletedTask;
    }

    private void VisitSelectQueryNode(SelectQueryNode node)
    {
        node.Type = node.ColumnsListNode.ColumnsNodes[0].Type;
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectSubqueryConditionExpressionNode node, CancellationToken cancellationToken)
    {
        node.Type = DataType.Boolean;
        return ValueTask.CompletedTask;
    }
}
