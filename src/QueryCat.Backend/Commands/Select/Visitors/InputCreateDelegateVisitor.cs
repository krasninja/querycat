using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal class InputCreateDelegateVisitor : CreateDelegateVisitor
{
    private readonly SelectCommandContext _context;
    private readonly IRowsInput[] _rowsInputs;

    /// <inheritdoc />
    public InputCreateDelegateVisitor(
        ExecutionThread thread,
        SelectCommandContext context,
        params IRowsInput[] rowsInputs) : base(thread, new InputResolveTypesVisitor(thread, rowsInputs))
    {
        _context = context;
        _rowsInputs = rowsInputs;
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);

        // First try to find the column in provided rows inputs.
        foreach (var rowsInput in _rowsInputs)
        {
            var columnIndex = rowsInput.GetColumnIndexByName(node.Name, node.SourceName);
            if (columnIndex > -1)
            {
                NodeIdFuncMap[node.Id] = new FuncUnitRowsInputColumn(rowsInput, columnIndex);
                return;
            }
        }

        // Then fallback to search within context.
        if (_context.TryGetInputSourceByName(
                node.Name,
                node.SourceName,
                out var result)
            && result?.Input is IRowsInput resultRowsInput)
        {
            NodeIdFuncMap[node.Id] = new FuncUnitRowsInputColumn(resultRowsInput, result.ColumnIndex);
            return;
        }

        // Base logic (if any).
        base.Visit(node);
    }
}
