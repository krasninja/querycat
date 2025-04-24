using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.For;

internal sealed class ForCommandHandler : StatementsBlockFuncUnit
{
    private readonly string _variableName;
    private readonly IRowsIterator _rowsIterator;

    public ForCommandHandler(
        StatementsVisitor statementsVisitor,
        ProgramBodyNode bodyNode,
        string variable,
        IRowsIterator iterator) : base(statementsVisitor, bodyNode)
    {
        _variableName = variable;
        _rowsIterator = iterator;
    }

    /// <inheritdoc />
    public override async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        try
        {
            var scope = thread.PushScope();
            await _rowsIterator.ResetAsync(cancellationToken);
            while (await _rowsIterator.MoveNextAsync(cancellationToken))
            {
                scope.Variables[_variableName] = VariantValue.CreateFromObject(_rowsIterator.Current);
                await base.InvokeAsync(thread, cancellationToken);
            }
        }
        finally
        {
            thread.PopScope();
        }

        return VariantValue.Null;
    }
}
