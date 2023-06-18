using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatters storage.
/// </summary>
public static class FormattersInfo
{
    private static readonly Dictionary<string, Func<ExecutionThread, IRowsFormatter>> Formatters = new()
    {
        // File extensions.
        [".csv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv"),
        [".tsv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv",
                FunctionArguments.Create().Add("delimiter", '\t')),
        [".tab"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv",
                FunctionArguments.Create().Add("delimiter", '\t')),
        [".log"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv",
                FunctionArguments.Create().Add("delimiter", ' ').Add("delimiter_can_repeat", false)),
        [".json"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("json"),
        [".xml"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("xml"),
        [".xsd"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("xml"),

        // Content types.
        ["text/csv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv"),
        ["text/x-csv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv"),
        ["application/csv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv"),
        ["application/x-csv"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv"),
        ["text/tab-separated-values"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("csv",
                FunctionArguments.Create().Add("delimiter", '\t')),
        ["application/json"] = thread=>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("json"),
        ["application/xml"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("xml"),
        ["application/xhtml+xml"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("xml"),
        ["application/soap+xml"] = thread =>
            thread.FunctionsManager.CallFunction<IRowsFormatter>("xml"),
    };

    /// <summary>
    /// Create formatter by file extension or content type.
    /// </summary>
    /// <param name="id">File extension or content type.</param>
    /// <param name="thread">Execution thread.</param>
    /// <returns>Instance of <see cref="IRowsFormatter" /> or null.</returns>
    public static IRowsFormatter? CreateFormatter(string id, ExecutionThread thread)
    {
        if (Formatters.TryGetValue(id.ToLower(), out var factory))
        {
            return factory.Invoke(thread);
        }
        return null;
    }

    /// <summary>
    /// Register formatter.
    /// </summary>
    /// <param name="id">Identifier (file extension or content type).</param>
    /// <param name="formatterFunc">Delegate to create formatter.</param>
    public static void RegisterFormatter(string id, Func<ExecutionThread, IRowsFormatter> formatterFunc)
    {
        Formatters[id] = formatterFunc;
    }
}
