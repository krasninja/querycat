using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager with no implementation.
/// </summary>
public sealed class NullFunctionsManager : IFunctionsManager
{
    public static NullFunctionsManager Instance { get; } = new();

    /// <inheritdoc />
    public void RegisterAggregate<TAggregate>(Func<IExecutionThread, TAggregate> factory)
        where TAggregate : IAggregateFunction
    {
    }

    /// <inheritdoc />
    public void RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
    }

    /// <inheritdoc />
    public void RegisterFactory(Action<IFunctionsManager> registerFunction, bool postpone = true)
    {
    }

    /// <inheritdoc />
    public bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        functions = Array.Empty<IFunction>();
        return false;
    }

    /// <inheritdoc />
    public bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
    {
        aggregateFunction = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<IFunction> GetFunctions() => Array.Empty<IFunction>();

    /// <inheritdoc />
    public VariantValue CallFunction(IFunction function, FunctionCallArguments callArguments) => VariantValue.Null;
}
