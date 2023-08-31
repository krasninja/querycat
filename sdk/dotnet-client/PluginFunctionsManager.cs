using System;
using System.Collections.Generic;
using System.Linq;
using QueryCat.Backend.Core.Functions;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Plugin functions manager. This is the simplified and limited version of functions manager.
/// It does not support aggregates and does not parse functions signature. No overloading.
/// </summary>
public sealed class PluginFunctionsManager : FunctionsManager
{
    private readonly Dictionary<string, PluginFunction> _functions = new();

    /// <inheritdoc />
    public override void RegisterAggregate(Type type)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
        var firstBracketIndex = signature.IndexOf('(');
        if (firstBracketIndex < 0)
        {
            return;
        }
        var name = signature.Substring(0, firstBracketIndex).ToUpper();
        _functions[name] = new PluginFunction(
            name,
            signature,
            @delegate)
        {
            Description = description ?? string.Empty,
        };
    }

    /// <inheritdoc />
    public override void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        if (_functions.TryGetValue(name.ToUpper(), out var functionInfo))
        {
            functions = new[] { functionInfo };
            return true;
        }
        functions = Array.Empty<IFunction>();
        return false;
    }

    /// <inheritdoc />
    public override bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override IEnumerable<IFunction> GetFunctions() => _functions.Values;

    /// <summary>
    /// Get all functions signatures.
    /// </summary>
    /// <returns>Signatures strings.</returns>
    public List<string> GetSignatures() => _functions.Values.Select(f => f.Signature).ToList();

    /// <inheritdoc />
    public override VariantValue CallFunction(IFunction function, FunctionCallArguments callArguments)
    {
        throw ThrowNotImplementedException();
    }

    private Exception ThrowNotImplementedException()
    {
        return new QueryCatPluginException(ErrorType.GENERIC,
            "Plugins execution context does not support functions manager.");
    }
}
