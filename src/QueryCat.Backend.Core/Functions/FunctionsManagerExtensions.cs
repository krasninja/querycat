using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Extensions and helpers for <see cref="FunctionsManager" />.
/// </summary>
public static class FunctionsManagerExtensions
{
    /// <summary>
    /// Register function.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="FunctionsManager" />.</param>
    /// <param name="functionDelegate">Function delegate.</param>
    public static void RegisterFunction(this FunctionsManager functionsManager, FunctionDelegate functionDelegate)
    {
        var classAttributes = functionDelegate.Method.GetCustomAttributes<FunctionSignatureAttribute>().ToArray();
        if (!classAttributes.Any())
        {
            throw new QueryCatException($"Delegate must have {nameof(FunctionSignatureAttribute)}.");
        }

        foreach (var classAttribute in classAttributes)
        {
            var descriptionAttribute = functionDelegate.Method.GetCustomAttribute<DescriptionAttribute>();
            functionsManager.RegisterFunction(classAttribute.Signature, functionDelegate,
                descriptionAttribute != null ? descriptionAttribute.Description : string.Empty);
        }
    }

    /// <summary>
    /// Call the function by name.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="FunctionsManager" />.</param>
    /// <param name="functionName">Function name.</param>
    /// <param name="arguments">Arguments to pass.</param>
    /// <returns>Result.</returns>
    public static VariantValue CallFunction(this FunctionsManager functionsManager,
        string functionName, FunctionCallArguments? arguments = null)
    {
        arguments ??= FunctionCallArguments.Empty;
        var function = functionsManager.FindByName(functionName, arguments.GetTypes());
        return functionsManager.CallFunction(function, arguments);
    }

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    /// <param name="functionsManager">Instance of <see cref="FunctionsManager" />.</param>
    /// <param name="type">Target type.</param>
    public static void RegisterFromType(this FunctionsManager functionsManager, Type type)
    {
        string GetFunctionNameWithAlternate(string signature, MemberInfo memberInfo)
        {
            var functionName = GetFunctionName(signature);
            if (string.IsNullOrEmpty(functionName))
            {
                functionName = FunctionFormatter.ToSnakeCase(memberInfo.Name);
            }
            return functionName;
        }

        // Try to register class as function.
        var classAttributes = type.GetCustomAttributes<FunctionSignatureAttribute>().ToArray();
        if (classAttributes.Any())
        {
            foreach (var classAttribute in classAttributes)
            {
                var firstConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (firstConstructor != null)
                {
                    var functionName = GetFunctionNameWithAlternate(classAttribute.Signature, type);
                    var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, firstConstructor.GetParameters(), typeof(object));
                    var @delegate = FunctionFormatter.CreateDelegateFromMethod(firstConstructor);
                    functionsManager.RegisterFunction(signature, @delegate);
                }
            }
            return;
        }

        // Try to register aggregates from type.
        if (typeof(IAggregateFunction).IsAssignableFrom(type))
        {
            functionsManager.RegisterAggregate(type);
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
            // The standard case: VariantValue FunctionName(FunctionCallInfo args).
            if (methodParameters.Length == 1
                && methodParameters[0].ParameterType == typeof(FunctionCallInfo)
                && method.ReturnType == typeof(VariantValue))
            {
                var args = Expression.Parameter(typeof(FunctionCallInfo), "input");
                var func = Expression.Lambda<FunctionDelegate>(Expression.Call(method, args), args).Compile();
                functionsManager.RegisterFunction(methodSignature.Signature, func);
            }
            // Non-standard case. Construct signature from function definition.
            else
            {
                var functionName = GetFunctionNameWithAlternate(methodSignature.Signature, method);
                var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
                var @delegate = FunctionFormatter.CreateDelegateFromMethod(method);
                functionsManager.RegisterFunction(signature, @delegate);
            }
        }
    }

    private static string GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf("(", StringComparison.Ordinal);
        if (indexOfLeftParen < 0)
        {
            return signature;
        }
        var name = signature[..indexOfLeftParen];
        if (name.StartsWith('['))
        {
            name = name.Substring(1, name.Length - 2);
        }
        return name;
    }
}
