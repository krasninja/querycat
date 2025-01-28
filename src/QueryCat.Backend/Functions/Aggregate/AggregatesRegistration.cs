using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions.Aggregate;

/// <summary>
/// Aggregates functions registration module.
/// </summary>
internal static class AggregatesRegistration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<AvgAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<CountAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<FirstValueAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<LastValueAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<MaxAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<MinAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<SumAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<StringAggAggregateFunction>());
        functionsManager.RegisterFunctions(functionsManager.Factory.CreateAggregateFromType<RowNumberAggregateFunction>());
    }
}
