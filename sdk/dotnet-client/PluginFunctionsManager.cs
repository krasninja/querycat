using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly Dictionary<string, IFunction> _functions = new();

    /// <inheritdoc />
    public FunctionsFactory Factory { get; } = new PluginFunctionsFactory();

    /// <inheritdoc />
    public IFunction ResolveUri(string uri)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void RegisterFunction(IFunction function)
    {
        _functions[function.Name] = function;
    }

    /// <inheritdoc />
    public IFunction[] FindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null)
    {
        name = FunctionFormatter.NormalizeName(name);
        if (_functions.TryGetValue(name, out var functionInfo))
        {
            return [functionInfo];
        }
        return [];
    }

    /// <inheritdoc />
    public IEnumerable<IFunction> GetFunctions() => _functions.Values;

    /// <summary>
    /// Get all functions signatures.
    /// </summary>
    /// <returns>Signatures strings.</returns>
    public IEnumerable<IFunction> GetPluginFunctions() => _functions.Values;

    /// <inheritdoc />
    public ValueTask<VariantValue> CallFunctionAsync(
        IFunction function,
        IExecutionThread executionThread,
        FunctionCallArguments callArguments,
        CancellationToken cancellationToken = default)
    {
        throw ThrowNotImplementedException();
    }

    private Exception ThrowNotImplementedException()
    {
        return new QueryCatPluginException(ErrorType.GENERIC, Resources.Errors.NotSupported_FunctionsManager);
    }
}
