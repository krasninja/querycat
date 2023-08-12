using System;
using System.Collections.Generic;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class PluginFunctionsManager : FunctionsManager
{
    /// <inheritdoc />
    public override void RegisterAggregate<T>()
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFunction(FunctionDelegate functionDelegate)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override Function RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFromType(Type type)
    {
        throw ThrowNotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindByName(string name, FunctionArgumentsTypes? functionArgumentsTypes, out Function[] functions)
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
    public override VariantValue CallFunction(Function function, FunctionArguments arguments)
    {
        throw ThrowNotImplementedException();
    }

    private Exception ThrowNotImplementedException()
    {
        return new QueryCatPluginException(ErrorType.GENERIC,
            "Plugins execution context does not support functions manager.");
    }
}
