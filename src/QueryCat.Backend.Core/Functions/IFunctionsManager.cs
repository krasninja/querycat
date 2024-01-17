using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager. Allows to register, find and call functions.
/// </summary>
public interface IFunctionsManager
{
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
    void RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null);

    /// <summary>
    /// Register the delegate that describes more functions.
    /// </summary>
    /// <param name="registerFunction">Register function delegate.</param>
    /// <param name="postpone">Postpone actual registration and add to pending list instead.</param>
    void RegisterFactory(Action<IFunctionsManager> registerFunction, bool postpone = true);

    /// <summary>
    /// Tries to find the function by name and it arguments types.
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
