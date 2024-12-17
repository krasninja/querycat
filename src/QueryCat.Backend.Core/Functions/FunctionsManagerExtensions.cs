using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
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
    /// Register function.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="functionDelegate">Function delegate.</param>
    public static void RegisterFunction(this IFunctionsManager functionsManager, FunctionDelegate functionDelegate)
    {
        var methodAttributes = Attribute.GetCustomAttributes(functionDelegate.Method, typeof(FunctionSignatureAttribute));
        if (methodAttributes.Length < 1)
        {
            throw new QueryCatException($"Delegate must have '{nameof(FunctionSignatureAttribute)}'.");
        }

        foreach (var attribute in methodAttributes)
        {
            var methodAttribute = (FunctionSignatureAttribute)attribute;
            var descriptionAttribute = functionDelegate.Method.GetCustomAttribute<DescriptionAttribute>();
            var formatterAttribute = functionDelegate.Method.GetCustomAttribute<FunctionFormattersAttribute>();
            functionsManager.RegisterFunction(
                methodAttribute.Signature,
                functionDelegate,
                description: descriptionAttribute != null ? descriptionAttribute.Description : string.Empty,
                formatterIds: formatterAttribute?.FormatterIds);
        }
    }

    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="functionDelegate">Function delegate.</param>
    public static void RegisterFunction(this IFunctionsManager functionsManager, Delegate functionDelegate)
        => RegisterFunctionFromMethodInfo(functionsManager, functionDelegate.Method);

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="type">Target type.</param>
    public static void RegisterFromType(
        this IFunctionsManager functionsManager,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        // Try to register class as function.
        var classAttributes = Attribute.GetCustomAttributes(type, typeof(FunctionSignatureAttribute));
        if (classAttributes.Any())
        {
            foreach (var classAttribute in classAttributes)
            {
                var firstConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (firstConstructor != null)
                {
                    var functionName = GetFunctionName(((FunctionSignatureAttribute)classAttribute).Signature, type);
                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }
                    var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, firstConstructor.GetParameters(), type);
                    var @delegate = FunctionFormatter.CreateDelegateFromMethod(firstConstructor);
                    var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                    functionsManager.RegisterFunction(signature, @delegate, description);
                }
            }
            return;
        }

        // Try to register aggregates from type.
        if (typeof(IAggregateFunction).IsAssignableFrom(type))
        {
            functionsManager.RegisterAggregate(() => (IAggregateFunction)Activator.CreateInstance(type)!);
            return;
        }

        // Try to register methods from type.
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var method in methods)
        {
            var methodSignature = method.GetCustomAttributes<FunctionSignatureAttribute>().FirstOrDefault();
            if (methodSignature == null)
            {
                continue;
            }

            var methodParameters = method.GetParameters();
            // The standard case: VariantValue FunctionName(IExecutionThread thread).
            if (methodParameters.Length == 1
                && methodParameters[0].ParameterType == typeof(IExecutionThread)
                && method.ReturnType == typeof(VariantValue))
            {
                var args = Expression.Parameter(typeof(IExecutionThread), "input");
                var func = Expression.Lambda<FunctionDelegate>(Expression.Call(method, args), args).Compile();
                var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                functionsManager.RegisterFunction(methodSignature.Signature, func, description);
            }
            // Non-standard case. Construct signature from function definition.
            else
            {
                RegisterFunctionFromMethodInfo(functionsManager, method);
            }
        }
    }

    private static void RegisterFunctionFromMethodInfo(this IFunctionsManager functionsManager, MethodInfo method)
    {
        var methodSignature = method.GetCustomAttributes<FunctionSignatureAttribute>().FirstOrDefault();
        if (methodSignature == null)
        {
            return;
        }

        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        var functionName = GetFunctionName(methodSignature.Signature, method);
        var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
        var @delegate = FunctionFormatter.CreateDelegateFromMethod(method);
        functionsManager.RegisterFunction(signature, @delegate, description);
    }

    private static string GetFunctionName(string signature, MemberInfo memberInfo)
    {
        var functionName = GetFunctionName(signature);
        if (functionName.Length < 1)
        {
            functionName = FunctionFormatter.ToSnakeCase(memberInfo.Name);
        }
        return functionName.ToString();
    }

    private static ReadOnlySpan<char> GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.Ordinal);
        if (indexOfLeftParen < 0)
        {
            return signature;
        }
        return signature.AsSpan()[..indexOfLeftParen];
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
                throw new QueryCatException($"There is more than one signature for function '{name}'.");
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
    /// Find aggregate function by name.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="IFunctionsManager" />.</param>
    /// <param name="name">Function name.</param>
    /// <returns>Found aggregate function.</returns>
    public static IAggregateFunction FindAggregateByName(this IFunctionsManager functionsManager, string name)
    {
        if (functionsManager.TryFindAggregateByName(name, out var aggregateFunction) && aggregateFunction != null)
        {
            return aggregateFunction;
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
    /// <returns>Result.</returns>
    public static VariantValue CallFunction(
        this IFunctionsManager functionsManager,
        string functionName,
        IExecutionThread executionThread,
        FunctionCallArguments? arguments = null)
    {
        arguments ??= FunctionCallArguments.Empty;
        var function = functionsManager.FindByName(functionName, arguments.GetTypes());
        return functionsManager.CallFunction(function, executionThread, arguments);
    }
}
