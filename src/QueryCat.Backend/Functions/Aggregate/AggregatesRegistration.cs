using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
internal static class AggregatesRegistration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterAggregate(() => new AvgAggregateFunction());
        functionsManager.RegisterAggregate(() => new CountAggregateFunction());
        functionsManager.RegisterAggregate(() => new FirstValueAggregateFunction());
        functionsManager.RegisterAggregate(() => new LastValueAggregateFunction());
        functionsManager.RegisterAggregate(() => new MaxAggregateFunction());
        functionsManager.RegisterAggregate(() => new MinAggregateFunction());
        functionsManager.RegisterAggregate(() => new SumAggregateFunction());
        functionsManager.RegisterAggregate(() => new StringAggAggregateFunction());
        functionsManager.RegisterAggregate(() => new RowNumberAggregateFunction());
    }
}
