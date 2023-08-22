using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Providers register.
/// </summary>
internal static class Registration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(StandardInputOutput.Stdout);
        functionsManager.RegisterFunction(StandardInputOutput.Stdin);

        functionsManager.RegisterFunction(GenericInputOutput.Read);
        functionsManager.RegisterFunction(GenericInputOutput.Write);

        functionsManager.RegisterFunction(FileInputOutput.ReadFile);
        functionsManager.RegisterFunction(FileInputOutput.WriteFile);

        functionsManager.RegisterFunction(StringInput.ReadString);

        functionsManager.RegisterFunction(CurlInput.WGet);
    }
}
