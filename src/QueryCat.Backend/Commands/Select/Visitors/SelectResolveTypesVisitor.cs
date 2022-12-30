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
        AstTraversal.TypesToIgnore.Add(typeof(SelectQuerySpecificationNode));
        AstTraversal.TypesToIgnore.Add(typeof(SelectTableJoinedNode));
        AstTraversal.AcceptBeforeIgnore = true;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        if (_context.TryGetInput(node.Name, node.SourceName, out var input, out var columnIndex))
        {
            node.SetAttribute(AstAttributeKeys.InputColumnKey, input!.Columns[columnIndex]);
            node.SetAttribute(AstAttributeKeys.InputColumnIndexKey, columnIndex);
            node.SetDataType(input.Columns[columnIndex].DataType);
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
        if (!_context.TryGetInput(node.ColumnName, node.SourceName, out var input, out var columnIndex))
        {
            return;
        }

        node.SetAttribute(AstAttributeKeys.InputColumnKey, input!.Columns[columnIndex]);
        node.SetAttribute(AstAttributeKeys.InputColumnIndexKey, columnIndex);
        node.SetDataType(input.Columns[columnIndex].DataType);
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
