using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

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
