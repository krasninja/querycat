using QueryCat.Backend.Functions;
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
    /// <typeparam name="T">Aggregate type.</typeparam>
    public abstract void RegisterAggregate<T>() where T : IAggregateFunction;

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="functionDelegate">Function delegate.</param>
    public abstract void RegisterFunction(FunctionDelegate functionDelegate);

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    /// <param name="delegate">Function delegate.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>Instance of <see cref="Function" />.</returns>
    public abstract Function RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null);

    /// <summary>
    /// Register the delegate that describe more functions.
    /// </summary>
    /// <param name="registerFunction">Register function delegate.</param>
    /// <param name="postpone">Postpone actual registration and add to pending list instead.</param>
    public abstract void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true);

    /// <summary>
    /// Register functions, aggregate functions from the specific type.
    /// </summary>
    /// <param name="type">Type to analyze.</param>
    public abstract void RegisterFromType(Type type);

    /// <summary>
    /// Tries to find the function by name and it arguments types.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Function arguments types.</param>
    /// <param name="functions">Found functions.</param>
    /// <returns>Returns <c>true</c> if functions were found, <c>false</c> otherwise.</returns>
    public abstract bool TryFindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes,
        out Function[] functions);

    /// <summary>
    /// Find function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Argument types to find. Can be used to find the specific overload.</param>
    /// <returns>Instance of <see cref="Function" />.</returns>
    public Function FindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes = null)
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
    public abstract IEnumerable<Function> GetFunctions();

    /// <summary>
    /// Call function by name.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="arguments">Arguments to pass.</param>
    /// <returns>Result.</returns>
    public abstract VariantValue CallFunction(string functionName, FunctionArguments? arguments = null);
}
