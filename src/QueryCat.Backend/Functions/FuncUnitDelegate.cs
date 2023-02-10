using System.Runtime.CompilerServices;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal sealed class FuncUnitDelegate : FuncUnit
{
    private readonly Func<VariantValue> _func;

    /// <inheritdoc />
    public override DataType OutputType { get; }

    public FuncUnitDelegate(Func<VariantValue> func, DataType outputType)
    {
        _func = func;
        OutputType = outputType;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override VariantValue Invoke() => _func.Invoke();

    /// <inheritdoc />
    public override string ToString() => nameof(FuncUnitDelegate);
}
