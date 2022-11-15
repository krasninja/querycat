using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Format utilities.
/// </summary>
public static class FormatUtils
{
    private static readonly Dictionary<string, Func<IRowsFormatter>> FormatterByExtensionFactories = new()
    {
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
    };

    public static IRowsFormatter? GetFormatterByExtension(string extension)
    {
        if (FormatterByExtensionFactories.TryGetValue(extension.ToLower(), out var factory))
        {
            return factory.Invoke();
        }
        return null;
    }

    private static readonly Dictionary<string, Func<IRowsFormatter>> FormatterByContentTypeFactories = new()
    {
        ["text/csv"] = () => new DsvFormatter(),
        ["text/x-csv"] = () => new DsvFormatter(','),
        ["application/csv"] = () => new DsvFormatter(','),
        ["application/x-csv"] = () => new DsvFormatter(','),
        ["text/tab-separated-values"] = () => new DsvFormatter('\t'),
        ["application/json"] = () => new JsonFormatter(),
    };

    public static IRowsFormatter? GetFormatterByContentType(string contentType)
    {
        if (FormatterByContentTypeFactories.TryGetValue(contentType.ToLower(), out var factory))
        {
            return factory.Invoke();
        }
        return null;
    }
}
