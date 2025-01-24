using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions manager. Allows to register, find and call functions.
/// </summary>
public interface IFunctionsManager
{
    /// <summary>
    /// Factory to create instances of <see cref="IFunction" />.
    /// </summary>
    FunctionsFactory Factory { get; }

    /// <summary>
    /// Resolve URI into the specific function call.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <returns>Function or null if cannot be resolved.</returns>
    IFunction? ResolveUri(string uri);

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="function">Function to register.</param>
    void RegisterFunction(IFunction function);

    /// <summary>
    /// Tries to find the function by name and its arguments types.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Function arguments types.</param>
    /// <returns>Found functions.</returns>
    IFunction[] FindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null);

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result.</returns>
    ValueTask<VariantValue> CallFunctionAsync(
        IFunction function,
        IExecutionThread executionThread,
        FunctionCallArguments callArguments,
        CancellationToken cancellationToken = default);
}
