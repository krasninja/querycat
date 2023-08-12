using System;
using System.Collections.Generic;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using VariantValue = QueryCat.Backend.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class PluginFunctionsManager : FunctionsManager
{
    /// <inheritdoc />
    public override void RegisterAggregate<T>()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFunction(FunctionDelegate functionDelegate)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override Function RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void RegisterFromType(Type type)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindByName(string name, FunctionArgumentsTypes? functionArgumentsTypes, out Function[] functions)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool TryFindAggregateByName(string name, out IAggregateFunction aggregateFunction)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override IEnumerable<Function> GetFunctions()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override VariantValue CallFunction(Function function, FunctionArguments arguments)
    {
        throw new NotImplementedException();
    }
}
