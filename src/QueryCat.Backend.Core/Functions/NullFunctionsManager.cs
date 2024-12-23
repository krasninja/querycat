using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager with no implementation.
/// </summary>
public sealed class NullFunctionsManager : IFunctionsManager
{
    /// <summary>
    /// Instance of <see cref="NullFunctionsManager" />.
    /// </summary>
    public static NullFunctionsManager Instance { get; } = new();

    /// <inheritdoc />
    public IFunction? ResolveUri(string uri) => null;

    /// <inheritdoc />
    public void RegisterAggregate<TAggregate>(Func<TAggregate> factory)
        where TAggregate : IAggregateFunction
    {
    }

    /// <inheritdoc />
    public string RegisterFunction(
        string signature,
        FunctionDelegate @delegate,
        string? description = null,
        string[]? formatterIds = null)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        functions = [];
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
    public VariantValue CallFunction(IFunction function, IExecutionThread executionThread,
        FunctionCallArguments callArguments) => VariantValue.Null;
}
