using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Addons.Formatters;

public static class AdditionalRegistration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        ClefFormatter.RegisterFunctions(functionsManager);
        GrokFormatter.RegisterFunctions(functionsManager);
        IISW3CFormatter.RegisterFunctions(functionsManager);
        JsonFormatter.RegisterFunctions(functionsManager);
        RegexpFormatter.RegisterFunctions(functionsManager);
        XmlFormatter.RegisterFunctions(functionsManager);
    }
}
