using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Extensions and helpers for <see cref="IFunctionsManager" />.
/// </summary>
public static class FunctionsManagerExtensions
{
    #region Registration

    /// <summary>
    /// Register multiple functions.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="functions">Functions to register.</param>
    public static void RegisterFunctions(this IFunctionsManager functionsManager, ReadOnlySpan<IFunction> functions)
    {
        foreach (var function in functions)
        {
            functionsManager.RegisterFunction(function);
        }
    }

    /// <summary>
    /// Register function from delegate.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="functionDelegate">Delegate.</param>
    public static void RegisterFunction(this IFunctionsManager functionsManager, Delegate functionDelegate)
    {
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateFromDelegate(functionDelegate));
    }

    #endregion

    #region Find

    /// <summary>
    /// Find function by name.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Argument types to find. Can be used to find the specific overload.</param>
    /// <returns>Instance of <see cref="IFunction" />.</returns>
    public static IFunction FindByName(
        this IFunctionsManager functionsManager,
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null)
    {
        if (functionsManager.TryFindByName(name, functionArgumentsTypes, out var functions))
        {
            if (functions.Length > 1 && functionArgumentsTypes != null)
            {
                throw new QueryCatException(
                    string.Format(Resources.Errors.FunctionWithMoreThanOneSignature, name));
            }
            return functions[0];
        }
        if (functionArgumentsTypes != null)
        {
            throw new CannotFindFunctionException(name, functionArgumentsTypes);
        }
        throw new CannotFindFunctionException(name);
    }

    /// <summary>
    /// Find aggregate function by name.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="name">Function name.</param>
    /// <returns>Found aggregate function.</returns>
    public static IAggregateFunction FindAggregateByName(this IFunctionsManager functionsManager, string name)
    {
        name = FunctionFormatter.NormalizeName(name);
        if (functionsManager.TryFindByName(name, null, out var functions))
        {
            foreach (var function in functions)
            {
                if (!function.IsAggregate)
                {
                    continue;
                }
                var value = (VariantValue)functions[0].Delegate.DynamicInvoke(NullExecutionThread.Instance)!;
                return value.AsRequired<IAggregateFunction>();
            }
        }

        throw new CannotFindFunctionException(name);
    }

    #endregion

    /// <summary>
    /// Call the function by name.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="functionName">Function name.</param>
    /// <param name="executionThread">Execution thread.</param>
    /// <param name="arguments">Arguments to pass.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result.</returns>
    public static ValueTask<VariantValue> CallFunctionAsync(
        this IFunctionsManager functionsManager,
        string functionName,
        IExecutionThread executionThread,
        FunctionCallArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        arguments ??= FunctionCallArguments.Empty;
        var function = functionsManager.FindByName(functionName, arguments.GetTypes());
        return functionsManager.CallFunctionAsync(function, executionThread, arguments, cancellationToken);
    }
}
