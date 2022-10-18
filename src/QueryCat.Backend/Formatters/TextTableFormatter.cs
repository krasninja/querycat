using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Simple text table formatter.
/// </summary>
public class TextTableFormatter : IRowsFormatter
{
    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new TextTableInput(new StreamReader(input));

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new TextTableOutput(output);
}
