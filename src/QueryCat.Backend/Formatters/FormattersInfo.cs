using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatters storage.
/// </summary>
public static class FormattersInfo
{
    private static readonly Dictionary<string, Func<FunctionsManager, FunctionArguments, IRowsFormatter>> Formatters = new()
    {
        // File extensions.
        [".csv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args),
        [".tsv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')),
        [".tab"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')),
        [".log"] = (fm, args) =>
            fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", ' ').Add("delimiter_can_repeat", false)),
        [".json"] = (fm, args) => fm.CallFunction<IRowsFormatter>("json", args),
        [".xml"] = (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args),
        [".xsd"] = (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args),

        // Content types.
        ["text/csv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args),
        ["text/x-csv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args),
        ["application/csv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args),
        ["application/x-csv"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args),
        ["text/tab-separated-values"] = (fm, args) => fm.CallFunction<IRowsFormatter>("csv", args.Add("delimiter", '\t')),
        ["application/json"] = (fm, args)=> fm.CallFunction<IRowsFormatter>("json", args),
        ["application/xml"] = (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args),
        ["application/xhtml+xml"] = (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args),
        ["application/soap+xml"] = (fm, args) => fm.CallFunction<IRowsFormatter>("xml", args),
    };

    /// <summary>
    /// Create formatter by file extension or content type.
    /// </summary>
    /// <param name="id">File extension or content type.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Instance of <see cref="IRowsFormatter" /> or null.</returns>
    public static IRowsFormatter? CreateFormatter(string id, ExecutionThread thread, FunctionArguments? args = null)
    {
        if (Formatters.TryGetValue(id.ToLower(), out var factory))
        {
            return factory.Invoke(thread.FunctionsManager, args ?? new FunctionArguments());
        }
        return null;
    }

    /// <summary>
    /// Register formatter.
    /// </summary>
    /// <param name="id">Identifier (file extension or content type).</param>
    /// <param name="formatterFunc">Delegate to create formatter.</param>
    public static void RegisterFormatter(string id, Func<FunctionsManager, FunctionArguments, IRowsFormatter> formatterFunc)
    {
        Formatters[id] = formatterFunc;
    }
}
