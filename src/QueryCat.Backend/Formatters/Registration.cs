using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Formatters;

internal static class Registration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        DsvFormatter.RegisterFunctions(functionsManager);
        NullFormatter.RegisterFunctions(functionsManager);
        TextLineFormatter.RegisterFunctions(functionsManager);

        // File extensions.
        FormattersInfo.RegisterFormatter(".tsv", (fm, et, args) =>
            fm.CallFunction("csv", et, args.Add("delimiter", '\t')).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".tab", (fm, et, args) =>
            fm.CallFunction("csv", et, args.Add("delimiter", '\t')).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".log", (fm, et, args) =>
            fm.CallFunction("csv", et, args.Add("delimiter", ' ').Add("delimiter_can_repeat", true)).AsRequired<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("text/tab-separated-values",
            (fm, et, args) => fm.CallFunction("csv", et, args.Add("delimiter", '\t')).AsRequired<IRowsFormatter>());
    }
}
