using System;
using System.Collections.Generic;
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
        var methodSignatureAttributes = Attribute.GetCustomAttributes(functionDelegate.Method,
            typeof(FunctionSignatureAttribute));
        var metadata = FunctionMetadata.CreateFromAttributes(functionDelegate.Method);
        foreach (var attribute in methodSignatureAttributes)
        {
            var methodAttribute = (FunctionSignatureAttribute)attribute;
            yield return new PluginFunction(functionDelegate, methodAttribute.Signature, metadata);
        }
    }

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        FunctionMetadata? metadata = null)
    {
        return new PluginFunction(functionDelegate, signature, metadata);
    }

    /// <inheritdoc />
    public override IEnumerable<IFunction> CreateAggregateFromType<TAggregate>() => [];
}
