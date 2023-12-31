using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// DSV input/output options.
/// </summary>
internal sealed class DsvOptions
{
    private const char QuoteChar = '\"';

    /// <summary>
    /// Stream.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Input options for stream rows input.
    /// </summary>
    public StreamRowsInputOptions InputOptions { get; set; } = new()
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            QuoteChars = new[] { QuoteChar },
        }
    };

    /// <summary>
    /// If file has header. If null - the formatter tries to determine it by analyzing first rows.
    /// </summary>
    public bool? HasHeader { get; init; }

    /// <summary>
    /// Quote all strings.
    /// </summary>
    public bool QuoteStrings { get; init; }

    public DsvOptions(Stream stream)
    {
        Stream = stream;
    }
}
