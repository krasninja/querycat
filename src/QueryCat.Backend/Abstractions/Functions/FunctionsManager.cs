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

    public abstract void RegisterFromType(Type type);

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    public void RegisterFromType<T>() => RegisterFromType(typeof(T));

    /// <summary>
    /// Find function by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Argument types to find. Can be used to find the specific overload.</param>
    /// <returns>Instance of <see cref="Function" />.</returns>
    public abstract Function FindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes = null);

    public abstract IAggregateFunction FindAggregateByName(string name);

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

    /// <summary>
    /// Call function with arguments.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="arguments">Call arguments.</param>
    /// <typeparam name="T">Return type.</typeparam>
    /// <returns>Return result.</returns>
    public T CallFunction<T>(string functionName, FunctionArguments? arguments = null)
        => CallFunction(functionName, arguments).As<T>();
}
