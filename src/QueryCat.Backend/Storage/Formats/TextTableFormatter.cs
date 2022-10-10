namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Simple text table formatter.
/// </summary>
public class TextTableFormatter : IRowsFormatter
{
    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new TextTableOutput(output);
}
