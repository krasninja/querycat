using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager with no implementation.
/// </summary>
public sealed class NullFunctionsManager : FunctionsManager
{
    public static NullFunctionsManager Instance { get; } = new();

    /// <inheritdoc />
    public override void RegisterAggregate<TAggregate>(Func<IExecutionThread, TAggregate> factory)
    {
    }

    /// <inheritdoc />
    public override void RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
    }

    /// <inheritdoc />
    public override void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
    }

    /// <inheritdoc />
    public override bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        functions = Array.Empty<IFunction>();
        return false;
    }

    /// <inheritdoc />
    public override bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
    {
        aggregateFunction = null;
        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<IFunction> GetFunctions() => Array.Empty<IFunction>();

    /// <inheritdoc />
    public override VariantValue CallFunction(IFunction function, FunctionCallArguments callArguments) => VariantValue.Null;
}
