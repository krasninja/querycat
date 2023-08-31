using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
internal static class AggregatesRegistration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate(typeof(AvgAggregateFunction));
        functionsManager.RegisterAggregate(typeof(CountAggregateFunction));
        functionsManager.RegisterAggregate(typeof(FirstValueAggregateFunction));
        functionsManager.RegisterAggregate(typeof(LastValueAggregateFunction));
        functionsManager.RegisterAggregate(typeof(MaxAggregateFunction));
        functionsManager.RegisterAggregate(typeof(MinAggregateFunction));
        functionsManager.RegisterAggregate(typeof(SumAggregateFunction));
        functionsManager.RegisterAggregate(typeof(StringAggAggregateFunction));
        functionsManager.RegisterAggregate(typeof(RowNumberAggregateFunction));
    }
}
