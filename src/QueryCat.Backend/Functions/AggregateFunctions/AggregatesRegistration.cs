namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
public static class AggregatesRegistration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate<AvgAggregateFunction>();
        functionsManager.RegisterAggregate<CountAggregateFunction>();
        functionsManager.RegisterAggregate<MaxAggregateFunction>();
        functionsManager.RegisterAggregate<MinAggregateFunction>();
        functionsManager.RegisterAggregate<SumAggregateFunction>();
    }
}
