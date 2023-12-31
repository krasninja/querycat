using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncCommandHandler : IFuncUnit
{
    private readonly Func<VariantValue> _func;

    public static FuncCommandHandler NullHandler { get; } = new(() => VariantValue.Null);

    public FuncCommandHandler(Func<VariantValue> func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    /// <inheritdoc />
    public VariantValue Invoke() => _func.Invoke();
}
