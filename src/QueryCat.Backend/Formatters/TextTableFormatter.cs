using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Simple text table formatter.
/// </summary>
public class TextTableFormatter : IRowsFormatter
{
    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
        => new TextTableInput(blob.GetStream(), key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
        => new TextTableOutput(blob.GetStream());
}
