using System.Runtime.CompilerServices;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal sealed class FuncUnitMultiDelegate : FuncUnit
{
    private readonly IFuncUnit[] _functions;

    public FuncUnitMultiDelegate(params IFuncUnit[] funcUnits)
    {
        _functions = funcUnits;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override VariantValue Invoke()
    {
        var lastResult = VariantValue.Null;
        for (var i = 0; i < _functions.Length; i++)
        {
            lastResult = _functions[i].Invoke();
        }
        return lastResult;
    }

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitMultiDelegate);
}
