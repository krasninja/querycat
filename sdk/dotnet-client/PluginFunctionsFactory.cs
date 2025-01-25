using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Plugins.Client;

public sealed class PluginFunctionsFactory : FunctionsFactory
{
    /// <inheritdoc />
    public override IEnumerable<IFunction> CreateFromDelegate(Delegate functionDelegate)
    {
        if (!FunctionCaller.IsValidFunctionDelegate(functionDelegate))
        {
            var function = CreateFunctionFromMethodInfo(functionDelegate.Method);
            return function != null ? [function] : [];
        }
        else
        {
            return CreateFunctionFromMethodAttributes(functionDelegate);
        }
    }

    private IEnumerable<IFunction> CreateFunctionFromMethodAttributes(Delegate functionDelegate)
    {
        var methodAttributes = Attribute.GetCustomAttributes(functionDelegate.Method, typeof(FunctionSignatureAttribute));
        var descriptionAttribute = functionDelegate.Method.GetCustomAttribute<DescriptionAttribute>();
        var formatterAttribute = functionDelegate.Method.GetCustomAttribute<FunctionFormattersAttribute>();
        var isSafeAttribute = functionDelegate.Method.GetCustomAttribute<SafeFunctionAttribute>();
        foreach (var attribute in methodAttributes)
        {
            var methodAttribute = (FunctionSignatureAttribute)attribute;

            yield return new PluginFunction(functionDelegate, methodAttribute.Signature)
            {
                Description = descriptionAttribute != null ? descriptionAttribute.Description : string.Empty,
                IsSafe = isSafeAttribute != null,
                Formatters = formatterAttribute != null ? formatterAttribute.FormatterIds : [],
            };
        }
    }

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        string? description = null,
        bool isSafe = false,
        string[]? formatters = null)
    {
        return new PluginFunction(functionDelegate, signature)
        {
            Description = description ?? string.Empty,
            IsSafe = isSafe,
            Formatters = formatters ?? [],
        };
    }

    /// <inheritdoc />
    public override IEnumerable<IFunction> CreateAggregateFromType(Type aggregateType)
    {
        yield break;
    }
}
