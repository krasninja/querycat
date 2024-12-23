using System;
using System.Collections.Generic;
using System.Reflection;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Plugin functions manager. This is the simplified and limited version of functions manager.
/// It does not support aggregates and does not parse functions signature. No overloading.
/// </summary>
public sealed class PluginFunctionsManager : IFunctionsManager
{
    private readonly Dictionary<string, PluginFunction> _functions = new();

    /// <inheritdoc />
    public IFunction ResolveUri(string uri)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void RegisterAggregate<TAggregate>(Func<TAggregate> factory)
        where TAggregate : IAggregateFunction
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public string RegisterFunction(
        string signature,
        FunctionDelegate @delegate,
        string? description = null,
        string[]? formatterIds = null)
    {
        var firstBracketIndex = signature.IndexOf('(');
        if (firstBracketIndex < 0)
        {
            return string.Empty;
        }
        var name = signature.Substring(0, firstBracketIndex).ToUpper();
        _functions[name] = new PluginFunction(
            name,
            signature,
            @delegate,
            formatterIds)
        {
            Description = description ?? string.Empty,
            IsSafe = @delegate.Method.GetCustomAttribute<SafeFunctionAttribute>() != null,
        };
        return name;
    }

    /// <inheritdoc />
    public bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        if (_functions.TryGetValue(name.ToUpper(), out var functionInfo))
        {
            functions = [functionInfo];
            return true;
        }
        functions = [];
        return false;
    }

    /// <inheritdoc />
    public bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<IFunction> GetFunctions() => _functions.Values;

    /// <summary>
    /// Get all functions signatures.
    /// </summary>
    /// <returns>Signatures strings.</returns>
    public IEnumerable<PluginFunction> GetPluginFunctions() => _functions.Values;

    /// <inheritdoc />
    public VariantValue CallFunction(IFunction function, IExecutionThread executionThread, FunctionCallArguments callArguments)
    {
        throw ThrowNotImplementedException();
    }

    private Exception ThrowNotImplementedException()
    {
        return new QueryCatPluginException(ErrorType.GENERIC, Resources.Errors.NotSupported_FunctionsManager);
    }
}
