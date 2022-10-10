namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
public static class AggregatesRegistration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate(new AvgAggregateFunction());
        functionsManager.RegisterAggregate(new CountAggregateFunction());
        functionsManager.RegisterAggregate(new MaxAggregateFunction());
        functionsManager.RegisterAggregate(new MinAggregateFunction());
        functionsManager.RegisterAggregate(new SumAggregateFunction());
    }
}
