using QueryCat.Backend.Abstractions.Functions;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
internal static class AggregatesRegistration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate<AvgAggregateFunction>();
        functionsManager.RegisterAggregate<CountAggregateFunction>();
        functionsManager.RegisterAggregate<FirstValueAggregateFunction>();
        functionsManager.RegisterAggregate<LastValueAggregateFunction>();
        functionsManager.RegisterAggregate<MaxAggregateFunction>();
        functionsManager.RegisterAggregate<MinAggregateFunction>();
        functionsManager.RegisterAggregate<SumAggregateFunction>();
        functionsManager.RegisterAggregate<StringAggAggregateFunction>();
        functionsManager.RegisterAggregate<RowNumberAggregateFunction>();
    }
}
