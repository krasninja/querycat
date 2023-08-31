using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Formatters;

public static class AdditionalRegistration
{
    public static void Register(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(SubRipFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(XmlFormatter.RegisterFunctions);
        functionsManager.RegisterFunction(IISW3CFormatter.IISW3C);

        FormattersInfo.RegisterFormatter(".json", (fm, args) => fm.CallFunction("json", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".srt", (fm, args) => fm.CallFunction("srt", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".xml", (fm, args) => fm.CallFunction("xml", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".xsd", (fm, args) => fm.CallFunction("xml", args).As<IRowsFormatter>());

        FormattersInfo.RegisterFormatter("application/json", (fm, args) => fm.CallFunction("json", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/x-subrip", (fm, args) => fm.CallFunction("srt", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/xml", (fm, args) => fm.CallFunction("xml", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/xhtml+xml", (fm, args) => fm.CallFunction("xml", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/soap+xml", (fm, args) => fm.CallFunction("xml", args).As<IRowsFormatter>());
    }
}
