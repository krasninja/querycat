using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatters storage.
/// </summary>
public static class FormattersInfo
{
    private static readonly Dictionary<string, Func<IFunctionsManager, IExecutionThread, FunctionCallArguments, ValueTask<VariantValue>>> _formatters
        = new(capacity: 64);

    /// <summary>
    /// Create formatter by file extension or content type.
    /// </summary>
    /// <param name="id">File extension or content type.</param>
    /// <param name="thread">Execution thread.</param>
    /// <param name="args">Arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IRowsFormatter" /> or null.</returns>
    public static async ValueTask<IRowsFormatter?> CreateFormatterAsync(
        string id,
        IExecutionThread thread,
        FunctionCallArguments? args = null,
        CancellationToken cancellationToken = default)
    {
        if (_formatters.TryGetValue(id.ToLower(), out var factory))
        {
            var value = await factory.Invoke(thread.FunctionsManager, thread, args ?? FunctionCallArguments.Empty);
            return value.AsRequired<IRowsFormatter>();
        }
        return null;
    }

    /// <summary>
    /// Register formatter.
    /// </summary>
    /// <param name="id">Identifier (file extension or content type).</param>
    /// <param name="formatterFunc">Delegate to create formatter. It must return the object of type <see cref="IRowsFormatter" />.</param>
    public static void RegisterFormatter(string id,
        Func<IFunctionsManager, IExecutionThread, FunctionCallArguments, ValueTask<VariantValue>> formatterFunc)
    {
        _formatters[id] = formatterFunc;
    }
}
