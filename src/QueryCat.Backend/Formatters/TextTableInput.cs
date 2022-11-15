using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// The input that is optimized to read from STDIN.
/// </summary>
internal sealed class TextTableInput : DsvInput
{
    /// <inheritdoc />
    public TextTableInput(Stream stream)
        : base(new DsvOptions(stream)
        {
            InputOptions = new StreamRowsInputOptions
            {
                DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
                {
                    Delimiters = new[] { ' ' },
                    DelimitersCanRepeat = true,
                    QuoteChars = Array.Empty<char>(),
                    SkipEmptyLines = true,
                },
            }
        })
    {
    }
}
