using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectResolveTypesVisitor : ResolveTypesVisitor
{
    private readonly SelectCommandContext _context;

    /// <inheritdoc />
    public SelectResolveTypesVisitor(ExecutionThread executionThread, SelectCommandContext context) :
        base(executionThread)
    {
        _context = context;
        AstTraversal.TypesToIgnore.Add(typeof(SelectQueryNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectTableJoinedNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (VisitIdentifierNode(node, node.Name, node.SourceName))
        {
            return;
        }
        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        node.ExpressionNode.CopyTo<DataType>(AstAttributeKeys.TypeKey, node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNameNode node)
    {
        if (VisitIdentifierNode(node, node.ColumnName, node.SourceName))
        {
            return;
        }
        if (string.IsNullOrEmpty(node.SourceName))
        {
            if (SetDataTypeFromVariable(node, node.ColumnName))
            {
                return;
            }
        }
        throw new CannotFindIdentifierException(node.ColumnName);
    }

    private bool VisitIdentifierNode(IAstNode node, string name, string source)
    {
        if (!_context.TryGetInputSourceByName(name, source, out var result)
            || result == null)
        {
            return false;
        }

        node.SetAttribute(AstAttributeKeys.InputColumnKey, result.Input.Columns[result.ColumnIndex]);
        node.SetAttribute(AstAttributeKeys.InputColumnIndexKey, result.ColumnIndex);
        node.SetDataType(result.Input.Columns[result.ColumnIndex].DataType);
        return true;
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderBySpecificationNode node)
    {
        node.SetDataType(node.Expression.GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node) => VisitSelectQueryNode(node);

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node) => VisitSelectQueryNode(node);

    private void VisitSelectQueryNode(SelectQueryNode node)
    {
        node.SetDataType(node.ColumnsListNode.Columns[0].GetDataType());
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryConditionExpressionNode node)
    {
        node.SetDataType(DataType.Boolean);
    }
}
