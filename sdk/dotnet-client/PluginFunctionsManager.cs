using System;
using System.Collections.Generic;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class PluginFunctionsManager : FunctionsManager
{
    public record struct FunctionInfo(
        string Signature,
        string Description);

    private readonly List<FunctionInfo> _functions = new();

    public IReadOnlyList<FunctionInfo> Functions => _functions;

    /// <inheritdoc />
    public override void RegisterAggregate(Type type)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
        _functions.Add(new FunctionInfo(signature, description ?? string.Empty));
    }

    /// <inheritdoc />
    public override void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindAggregateByName(string name, out IAggregateFunction aggregateFunction)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override IEnumerable<Function> GetFunctions()
    {
        throw ThrowNotImplementedException();
    }

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
