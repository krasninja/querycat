using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal class InputCreateDelegateVisitor : CreateDelegateVisitor
{
    private readonly SelectCommandContext _context;
    private readonly IRowsInput _leftInput;
    private readonly IRowsInput _rightInput;
    private readonly IRowsInput[] _rowsInputs;

    /// <inheritdoc />
    public InputCreateDelegateVisitor(
        ExecutionThread thread,
        SelectCommandContext context,
        IRowsInput leftInput,
        IRowsInput rightInput) : base(thread, new InputResolveTypesVisitor(thread, leftInput, rightInput))
    {
        _context = context;
        _leftInput = leftInput;
        _rightInput = rightInput;
        _rowsInputs = [leftInput, rightInput];
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        ResolveTypesVisitor.Visit(node);
        var func = GetValueDelegateForIdentifier(node.Name, node.SourceName);
        if (func != null)
        {
            NodeIdFuncMap[node.Id] = func;
            return;
        }

        // Base logic (if any).
        base.Visit(node);
    }

    private IFuncUnit? GetValueDelegateForIdentifier(string name, string source)
    {
        // First try to find the column in provided rows inputs.
        foreach (var rowsInput in _rowsInputs)
        {
            var columnIndex = rowsInput.GetColumnIndexByName(name, source);
            if (columnIndex > -1)
            {
                return new FuncUnitRowsInputColumn(rowsInput, columnIndex);
            }
        }

        // Then fallback to search within context.
        if (_context.TryGetInputSourceByName(
                name,
                source,
                out var result)
            && result?.Input is IRowsInput resultRowsInput)
        {
            return new FuncUnitRowsInputColumn(resultRowsInput, result.ColumnIndex);
        }

        return null;
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedUsingNode node)
    {
        var leftColumnSources = new IFuncUnit[node.ColumnList.Count];
        var rightColumnSources = new IFuncUnit[node.ColumnList.Count];

        for (var i = 0; i < node.ColumnList.Count; i++)
        {
            var columnName = node.ColumnList[i];
            var leftFunc = GetValueDelegateForIdentifier(columnName, string.Empty);
            var rightColumnIndex = _rightInput.GetColumnIndexByName(columnName);
            if (rightColumnIndex < 0 || leftFunc == null)
            {
                throw new SemanticException($"Cannot find column '{columnName}'.");
            }
            leftColumnSources[i] = leftFunc;
            rightColumnSources[i] = new FuncUnitRowsInputColumn(_rightInput, rightColumnIndex);
        }

        VariantValue Func()
        {
            for (var i = 0; i < leftColumnSources.Length; i++)
            {
                var leftValue = leftColumnSources[i].Invoke();
                var rightValue = rightColumnSources[i].Invoke();
                if (leftValue != rightValue)
                {
                    return VariantValue.FalseValue;
                }
            }
            return VariantValue.TrueValue;
        }
        NodeIdFuncMap[node.Id] = new FuncUnitDelegate(Func, DataType.Boolean);
    }
}
