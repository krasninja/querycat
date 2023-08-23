using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions.Functions;

/// <summary>
/// Functions manager. Allows to register, find and call functions.
/// </summary>
public abstract class FunctionsManager
{
    /// <summary>
    /// Register aggregate function.
    /// </summary>
    public abstract void RegisterAggregate(Type type);

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    /// <param name="delegate">Function delegate.</param>
    /// <param name="description">Optional description.</param>
    public abstract void RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null);

    /// <summary>
    /// Register the delegate that describes more functions.
    /// </summary>
    /// <param name="registerFunction">Register function delegate.</param>
    /// <param name="postpone">Postpone actual registration and add to pending list instead.</param>
    public abstract void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true);

    /// <summary>
    /// Tries to find the function by name and it arguments types.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Function arguments types.</param>
    /// <param name="functions">Found functions.</param>
    /// <returns>Returns <c>true</c> if functions were found, <c>false</c> otherwise.</returns>
    public abstract bool TryFindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes,
        out IFunction[] functions);

    /// <summary>
    /// Find function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Argument types to find. Can be used to find the specific overload.</param>
    /// <returns>Instance of <see cref="IFunction" />.</returns>
    public IFunction FindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null)
    {
        if (TryFindByName(name, functionArgumentsTypes, out var functions))
        {
            if (functions.Length > 1 && functionArgumentsTypes != null)
            {
                throw new CannotFindFunctionException($"There is more than one signature for function '{name}'.");
            }
            return functions.First();
        }
        if (functionArgumentsTypes != null)
        {
            throw new CannotFindFunctionException(name, functionArgumentsTypes);
        }
        throw new CannotFindFunctionException(name);
    }

    /// <summary>
    /// Try to find aggregate function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="aggregateFunction">Found aggregate function.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public abstract bool TryFindAggregateByName(string name, out IAggregateFunction aggregateFunction);

    /// <summary>
    /// Find aggregate function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <returns>Found aggregate function.</returns>
    public IAggregateFunction FindAggregateByName(string name)
    {
        if (TryFindAggregateByName(name, out var aggregateFunction))
        {
            return aggregateFunction;
        }

        throw new CannotFindFunctionException(name);
    }

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Enumerable of all registered functions.</returns>
    public abstract IEnumerable<IFunction> GetFunctions();

    /// <summary>
    /// Call the function.
    /// </summary>
    /// <param name="function">Function.</param>
    /// <param name="callArguments">Arguments to pass.</param>
    /// <returns>Result.</returns>
    public abstract VariantValue CallFunction(IFunction function, FunctionCallArguments callArguments);
}
