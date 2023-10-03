using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Formatters;

internal static class Registration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFactory(DsvFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(NullFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(TextLineFormatter.RegisterFunctions);

        // File extensions.
        FormattersInfo.RegisterFormatter(".csv", (fm, args) => fm.CallFunction("csv", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".tsv", (fm, args) => fm.CallFunction("csv", args.Add("delimiter", '\t')).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".tab", (fm, args) => fm.CallFunction("csv", args.Add("delimiter", '\t')).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter(".log", (fm, args) =>
            fm.CallFunction("csv", args.Add("delimiter", ' ').Add("delimiter_can_repeat", true)).As<IRowsFormatter>());

        // Content types.
        FormattersInfo.RegisterFormatter("text/csv", (fm, args) => fm.CallFunction("csv", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("text/x-csv", (fm, args) => fm.CallFunction("csv", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/csv", (fm, args) => fm.CallFunction("csv", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("application/x-csv", (fm, args) => fm.CallFunction("csv", args).As<IRowsFormatter>());
        FormattersInfo.RegisterFormatter("text/tab-separated-values",
            (fm, args) => fm.CallFunction("csv", args.Add("delimiter", '\t')).As<IRowsFormatter>());
    }
}
