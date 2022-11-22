using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Command context implementation for function call.
/// </summary>
public class FunctionCommandContext : CommandContext
{
    private readonly IFuncUnit _funcUnit;

    public FunctionCommandContext(IFuncUnit funcUnit)
    {
        _funcUnit = funcUnit;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
        => _funcUnit.Invoke();
}
