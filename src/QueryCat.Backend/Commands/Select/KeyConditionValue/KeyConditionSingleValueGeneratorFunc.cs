using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionSingleValueGeneratorFunc : IKeyConditionSingleValueGenerator
{
    private readonly IFuncUnit _func;

    public KeyConditionSingleValueGeneratorFunc(IFuncUnit func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue?> GetAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        if (_func is FuncUnitRowsInputColumn rowsInputColumn)
        {
            var result = await rowsInputColumn.TryInvokeAsync(thread, out var localValue, cancellationToken);
            return result ? localValue : null;
        }
        return await _func.InvokeAsync(thread, cancellationToken);
    }
}
