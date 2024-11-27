using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

internal sealed class KeyConditionSingleValueGeneratorFunc : IKeyConditionSingleValueGenerator
{
    private readonly IFuncUnit _func;

    public KeyConditionSingleValueGeneratorFunc(IFuncUnit func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public VariantValue Get(IExecutionThread thread) => _func.Invoke(thread);
}
