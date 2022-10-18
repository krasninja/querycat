using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// The input that is optimized to read from STDIN.
/// </summary>
public sealed class TextTableInput : StreamRowsInput
{
    /// <inheritdoc />
    public TextTableInput(StreamReader streamReader)
        : base(streamReader, new DelimiterStreamReader.ReaderOptions
        {
            Delimiters = new[] { ' ' },
            QuoteChars = Array.Empty<char>(),
            DelimitersCanRepeat = true,
        })
    {
    }
}
