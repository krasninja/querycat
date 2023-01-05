using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Command context implementation for function call.
/// </summary>
public class FunctionCommandHandler : CommandHandler
{
    private readonly IFuncUnit _funcUnit;

    public FunctionCommandHandler(IFuncUnit funcUnit)
    {
        _funcUnit = funcUnit;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
        => _funcUnit.Invoke();
}
