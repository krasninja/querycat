using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatters storage.
/// </summary>
public static class FormattersInfo
{
    private static readonly Dictionary<string, Func<IRowsFormatter>> Formatters = new()
    {
        // File extensions.
        [".csv"] = () => new DsvFormatter(),
        [".tsv"] = () => new DsvFormatter('\t'),
        [".tab"] = () => new DsvFormatter('\t'),
        [".log"] = () => new DsvFormatter(new StreamRowsInputOptions
        {
            DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
            {
                Delimiters = new[] { ' ' },
                DelimitersCanRepeat = true,
                SkipEmptyLines = true,
            }
        }),
        [".json"] = () => new JsonFormatter(),
        [".xml"] = () => new XmlFormatter(),

        // Content types.
        ["text/csv"] = () => new DsvFormatter(),
        ["text/x-csv"] = () => new DsvFormatter(','),
        ["application/csv"] = () => new DsvFormatter(','),
        ["application/x-csv"] = () => new DsvFormatter(','),
        ["text/tab-separated-values"] = () => new DsvFormatter('\t'),
        ["application/json"] = () => new JsonFormatter(),
        ["application/xml"] = () => new XmlFormatter(),
        ["application/xhtml+xml"] = () => new XmlFormatter(),
        ["application/soap+xml"] = () => new XmlFormatter(),
    };

    /// <summary>
    /// Create formatter by file extension or content type.
    /// </summary>
    /// <param name="id">File extension or content type.</param>
    /// <returns>Instance of <see cref="IRowsFormatter" /> or null.</returns>
    public static IRowsFormatter? CreateFormatter(string id)
    {
        if (Formatters.TryGetValue(id.ToLower(), out var factory))
        {
            return factory.Invoke();
        }
        return null;
    }

    /// <summary>
    /// Register formatter.
    /// </summary>
    /// <param name="id">Identifier (file extension or content type).</param>
    /// <param name="formatterFunc">Delegate to create formatter.</param>
    public static void RegisterFormatter(string id, Func<IRowsFormatter> formatterFunc)
    {
        Formatters[id] = formatterFunc;
    }
}
