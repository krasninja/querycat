using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

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
                functionName = ToSnakeCase(memberInfo.Name);
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
                    var proxy = new MethodFunctionProxy(firstConstructor, functionName);
                    var signature = FormatSignatureFromParameters(functionName, firstConstructor.GetParameters(), typeof(object));
                    functionsManager.RegisterFunction(signature, proxy.FunctionDelegate);
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
                var proxy = new MethodFunctionProxy(method, functionName);
                var signature = FormatSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
                functionsManager.RegisterFunction(signature, proxy.FunctionDelegate);
            }
        }
    }

    private static string FormatSignatureFromParameters(string name, ParameterInfo[] parameterInfos, Type outputType)
    {
        var args = parameterInfos.Select(
            p => ToSnakeCase(p.Name ?? string.Empty) + ": " + Converter.ConvertFromSystem(p.ParameterType));
        var signature = $"{name}({string.Join(", ", args)}): {Converter.ConvertFromSystem(outputType)}";
        return signature;
    }

    private static string ToSnakeCase(string target)
    {
        // Based on https://stackoverflow.com/questions/63055621/how-to-convert-camel-case-to-snake-case-with-two-capitals-next-to-each-other.
        var sb = new StringBuilder(capacity: target.Length)
            .Append(char.ToLower(target[0]));
        for (var i = 1; i < target.Length; ++i)
        {
            var ch = target[i];
            if (char.IsUpper(ch))
            {
                sb.Append('_');
                sb.Append(char.ToLower(ch));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
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
