using System.Runtime.CompilerServices;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal sealed class FuncUnitStatic : FuncUnit
{
    private readonly VariantValue _value;

    /// <inheritdoc />
    public override DataType OutputType => _value.GetInternalType();

    public FuncUnitStatic(VariantValue value)
    {
        _value = value;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override VariantValue Invoke() => _value;

    /// <inheritdoc />
    public override string ToString() => $"{nameof(FuncUnitStatic)}: {_value}";
}
