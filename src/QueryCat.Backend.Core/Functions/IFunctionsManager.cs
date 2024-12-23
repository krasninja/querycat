using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager. Allows to register, find and call functions.
/// </summary>
public interface IFunctionsManager
{
    /// <summary>
    /// Resolve URI into the specific function call.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <returns>Function or null if cannot be resolved.</returns>
    IFunction? ResolveUri(string uri);

    /// <summary>
    /// Register aggregate function.
    /// </summary>
    /// <param name="factory">Factory method that creates aggregate instance.</param>
    void RegisterAggregate<TAggregate>(Func<TAggregate> factory)
        where TAggregate : IAggregateFunction;

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    /// <param name="delegate">Function delegate.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="formatterIds">The extensions or MIME types that can be used for input.
    /// Can be applied to formatters.</param>
    /// <returns>Function name.</returns>
    string RegisterFunction(
        string signature,
        FunctionDelegate @delegate,
        string? description = null,
        string[]? formatterIds = null);

    /// <summary>
    /// Tries to find the function by name and its arguments types.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Function arguments types.</param>
    /// <param name="functions">Found functions.</param>
    /// <returns>Returns <c>true</c> if functions were found, <c>false</c> otherwise.</returns>
    bool TryFindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes,
        out IFunction[] functions);

    /// <summary>
    /// Try to find aggregate function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="aggregateFunction">Found aggregate function.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction);

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Enumerable of all registered functions.</returns>
    IEnumerable<IFunction> GetFunctions();

    /// <summary>
    /// Call the function.
    /// </summary>
    /// <param name="function">Function.</param>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="callArguments">Arguments to pass.</param>
    /// <returns>Result.</returns>
    VariantValue CallFunction(IFunction function, IExecutionThread executionThread, FunctionCallArguments callArguments);
}
