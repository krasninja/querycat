using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Formatters;

internal static class Registration
{
    public static void Register(IFunctionsManager functionsManager)
    {
        DsvFormatter.RegisterFunctions(functionsManager);
        NullRowsFormatter.RegisterFunctions(functionsManager);
        TextLineFormatter.RegisterFunctions(functionsManager);

        // File extensions.
        FormattersInfo.RegisterFormatter(".tsv", (fm, et, args) => fm.CallFunctionAsync("csv", et, args.Add("delimiter", '\t')));
        FormattersInfo.RegisterFormatter(".tab", (fm, et, args) => fm.CallFunctionAsync("csv", et, args.Add("delimiter", '\t')));
        FormattersInfo.RegisterFormatter(".log", (fm, et, args) =>
            fm.CallFunctionAsync("csv", et, args.Add("delimiter", ' ').Add("delimiter_can_repeat", true)));
        FormattersInfo.RegisterFormatter("text/tab-separated-values",
            (fm, et, args) => fm.CallFunctionAsync("csv", et, args.Add("delimiter", '\t')));
    }
}
