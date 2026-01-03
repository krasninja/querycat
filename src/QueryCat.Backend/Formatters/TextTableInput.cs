using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// The input that is optimized to read from STDIN.
/// </summary>
internal sealed class TextTableInput : DsvInput
{
    /// <inheritdoc />
    public TextTableInput(Stream stream, string? key = null)
        : base(new DsvOptions(stream)
        {
            InputOptions = new StreamRowsInputOptions
            {
                DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
                {
                    Delimiters = [' '],
                    SkipRepeatedDelimiters = true,
                    QuoteChars = [],
                    SkipEmptyLines = true,
                    Culture = Application.Culture,
                },
            }
        }, key)
    {
    }
}
