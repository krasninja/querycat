using System.ComponentModel;
using System.Reflection;
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
        string functionName, FunctionArguments? arguments = null)
    {
        arguments ??= FunctionArguments.Empty;
        var function = functionsManager.FindByName(functionName, arguments.GetTypes());
        return functionsManager.CallFunction(function, arguments);
    }
}
