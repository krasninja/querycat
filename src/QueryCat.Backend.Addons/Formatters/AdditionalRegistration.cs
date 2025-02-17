using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Addons.Formatters;

public static class AdditionalRegistration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        JsonFormatter.RegisterFunctions(functionsManager);
        RegexpFormatter.RegisterFunctions(functionsManager);
        GrokFormatter.RegisterFunctions(functionsManager);
        XmlFormatter.RegisterFunctions(functionsManager);
        functionsManager.RegisterFunction(IISW3CFormatter.IISW3C);
    }
}
