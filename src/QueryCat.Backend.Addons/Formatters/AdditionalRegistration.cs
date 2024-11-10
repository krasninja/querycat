using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Formatters;

namespace QueryCat.Backend.Addons.Formatters;

public static class AdditionalRegistration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(RegexpFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(GrokFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(SubRipFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(XmlFormatter.RegisterFunctions);
        functionsManager.RegisterFunction(IISW3CFormatter.IISW3C);

        FormattersInfo.RegisterFormatter(".json", (fm, et, args) => fm.CallFunction("json", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".srt", (fm, et, args) => fm.CallFunction("srt", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".xml", (fm, et, args) => fm.CallFunction("xml", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".xsd", (fm, et, args) => fm.CallFunction("xml", et, args).AsRequired<IRowsFormatter>());

        FormattersInfo.RegisterFormatter("application/json", (fm, et, args) => fm.CallFunction("json", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/x-subrip", (fm, et, args) => fm.CallFunction("srt", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/xml", (fm, et, args) => fm.CallFunction("xml", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/xhtml+xml", (fm, et, args) => fm.CallFunction("xml", et, args).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/soap+xml", (fm, et, args) => fm.CallFunction("xml", et, args).AsRequired<IRowsFormatter>());
    }
}
