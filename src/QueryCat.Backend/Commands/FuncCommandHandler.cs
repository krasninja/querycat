using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class FuncCommandHandler : CommandHandler
{
    private readonly Func<VariantValue> _func;

    public static FuncCommandHandler NullHandler { get; } = new(() => VariantValue.Null);

    public FuncCommandHandler(Func<VariantValue> func)
    {
        _func = func;
    }

    /// <inheritdoc />
    public override VariantValue Invoke() => _func.Invoke();
}
