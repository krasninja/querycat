using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Functions;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatters storage.
/// </summary>
public static class FormattersInfo
{
    private static readonly Dictionary<string, Func<FunctionsManager, FunctionCallArguments, IRowsFormatter>> Formatters = new(capacity: 64);

    /// <summary>
    /// Create formatter by file extension or content type.
    /// </summary>
    /// <param name="id">File extension or content type.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Instance of <see cref="IRowsFormatter" /> or null.</returns>
    public static IRowsFormatter? CreateFormatter(string id, IExecutionThread thread, FunctionCallArguments? args = null)
    {
        if (Formatters.TryGetValue(id.ToLower(), out var factory))
        {
            return factory.Invoke(thread.FunctionsManager, args ?? new FunctionCallArguments());
        }
        return null;
    }

    /// <summary>
    /// Register formatter.
    /// </summary>
    /// <param name="id">Identifier (file extension or content type).</param>
    /// <param name="formatterFunc">Delegate to create formatter.</param>
    public static void RegisterFormatter(string id, Func<FunctionsManager, FunctionCallArguments, IRowsFormatter> formatterFunc)
    {
        Formatters[id] = formatterFunc;
    }
}
