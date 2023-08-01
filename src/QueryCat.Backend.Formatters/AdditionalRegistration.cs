using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Formatters;

public static class AdditionalRegistration
{
    public static void Register(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(XmlFormatter.RegisterFunctions);

        FormattersInfo.RegisterFormatter(".json", (fm, args) => fm.CallFunction<IRowsFormatter>("json", args));
        FormattersInfo.RegisterFormatter(".xml", (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args));
        FormattersInfo.RegisterFormatter(".xsd", (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args));

        FormattersInfo.RegisterFormatter("application/json", (fm, args) => fm.CallFunction<IRowsFormatter>("json", args));
        FormattersInfo.RegisterFormatter("application/xml", (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args));
        FormattersInfo.RegisterFormatter("application/xhtml+xml", (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args));
        FormattersInfo.RegisterFormatter("application/soap+xml", (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args));
    }
}
