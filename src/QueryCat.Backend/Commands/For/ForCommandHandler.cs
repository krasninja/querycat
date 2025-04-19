using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.For;

internal sealed class ForCommandHandler : IFuncUnit
{
    private readonly string _variableName;
    private readonly IRowsIterator _rowsIterator;
    private readonly IFuncUnit _loopFuncUnit;

    /// <inheritdoc />
    public DataType OutputType => DataType.Void;

    public ForCommandHandler(string variable, IRowsIterator iterator, IFuncUnit loopBodyFuncUnit)
    {
        _variableName = variable;
        _rowsIterator = iterator;
        _loopFuncUnit = loopBodyFuncUnit;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        try
        {
            var scope = thread.PushScope();
            await _rowsIterator.ResetAsync(cancellationToken);
            while (await _rowsIterator.MoveNextAsync(cancellationToken))
            {
                scope.Variables[_variableName] = VariantValue.CreateFromObject(_rowsIterator.Current);
                await _loopFuncUnit.InvokeAsync(thread, cancellationToken);
            }
        }
        finally
        {
            thread.PopScope();
        }

        return VariantValue.Null;
    }
}
