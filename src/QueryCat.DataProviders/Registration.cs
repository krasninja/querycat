using QueryCat.Backend.Functions;

namespace QueryCat.DataProviders;

/// <summary>
/// Functions registration.
/// </summary>
public static class Registration
{
    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(IIS.IISW3CFormatter.IISW3C);
    }
}
