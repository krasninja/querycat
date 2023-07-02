using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Simple text table formatter.
/// </summary>
public class TextTableFormatter : IRowsFormatter
{
    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
        => new TextTableInput(input, key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new TextTableOutput(output);
}
