using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Command context implementation for function call.
/// </summary>
internal sealed class FuncUnitCommandHandler : CommandHandler
{
    private readonly IFuncUnit _funcUnit;

    public FuncUnitCommandHandler(IFuncUnit funcUnit)
    {
        _funcUnit = funcUnit;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
        => _funcUnit.Invoke();
}
