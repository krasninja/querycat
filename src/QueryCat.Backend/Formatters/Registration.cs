using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Formatters;

internal static class Registration
{
    public static void Register(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFactory(DsvFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(NullFormatter.RegisterFunctions);
        functionsManager.RegisterFactory(TextLineFormatter.RegisterFunctions);

        // File extensions.
        FormattersInfo.RegisterFormatter(".csv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args));
        FormattersInfo.RegisterFormatter(".tsv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')));
        FormattersInfo.RegisterFormatter(".tab", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')));
        FormattersInfo.RegisterFormatter(".log", (fm, args) =>
            fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", ' ').Add("delimiter_can_repeat", true)));

        // Content types.
        FormattersInfo.RegisterFormatter("text/csv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args));
        FormattersInfo.RegisterFormatter("text/x-csv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args));
        FormattersInfo.RegisterFormatter("application/csv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args));
        FormattersInfo.RegisterFormatter("application/x-csv", (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args));
        FormattersInfo.RegisterFormatter("text/tab-separated-values",
            (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')));
    }
}
