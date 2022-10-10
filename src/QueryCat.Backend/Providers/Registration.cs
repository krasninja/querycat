using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Providers register.
/// </summary>
public static class Registration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ConsoleDataProviders.Console);

        functionsManager.RegisterFunction(GenericProvider.Read);
        functionsManager.RegisterFunction(GenericProvider.Write);

        functionsManager.RegisterFunction(FileDataProviders.ReadFile);
        functionsManager.RegisterFunction(FileDataProviders.WriteFile);

        functionsManager.RegisterFunction(StringDataProviders.ReadString);

        functionsManager.RegisterFunction(CurlDataProviders.WGet);
    }
}
