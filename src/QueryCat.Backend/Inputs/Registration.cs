using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Inputs;

internal static class Registration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(BufferRowsInput.BufferInput);
        functionsManager.RegisterFunction(BufferRowsOutput.BufferOutput);
        functionsManager.RegisterFunction(GenerateSeriesInput.GenerateSeries);
        functionsManager.RegisterFunction(ParallelRowsOutput.ParallelOutput);
        functionsManager.RegisterFunction(RetryRowsInput.RetryInput);
        functionsManager.RegisterFunction(RetryRowsOutput.RetryOutput);
    }
}
