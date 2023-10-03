using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
internal static class AggregatesRegistration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate(_ => new AvgAggregateFunction());
        functionsManager.RegisterAggregate(_ => new CountAggregateFunction());
        functionsManager.RegisterAggregate(_ => new FirstValueAggregateFunction());
        functionsManager.RegisterAggregate(_ => new LastValueAggregateFunction());
        functionsManager.RegisterAggregate(_ => new MaxAggregateFunction());
        functionsManager.RegisterAggregate(_ => new MinAggregateFunction());
        functionsManager.RegisterAggregate(_ => new SumAggregateFunction());
        functionsManager.RegisterAggregate(_ => new StringAggAggregateFunction());
        functionsManager.RegisterAggregate(_ => new RowNumberAggregateFunction());
    }
}
