using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Inputs;

internal static class Registration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(GenerateSeriesInput.GenerateSeries);
        functionsManager.RegisterFunction(RetryRowsInput.RetryInput);
        functionsManager.RegisterFunction(RetryRowsOutput.RetryOutput);
    }
}
