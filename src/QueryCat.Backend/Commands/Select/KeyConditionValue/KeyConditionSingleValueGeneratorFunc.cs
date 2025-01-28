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
    public bool TryGet(IExecutionThread thread, out VariantValue value)
    {
        if (_func is FuncUnitRowsInputColumn rowsInputColumn)
        {
            var localValue = VariantValue.Null;
            var result = AsyncUtils.RunSync(() => rowsInputColumn.TryInvokeAsync(thread, out localValue, CancellationToken.None));
            value = localValue;
            return result;
        }
        value = _func.Invoke(thread);
        return true;
    }
}
