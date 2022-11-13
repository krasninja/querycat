using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Options for <see cref="StreamRowsInput" />.
/// </summary>
public class StreamRowsInputOptions
{
    /// <summary>
    /// Quote character.
    /// </summary>
    public DelimiterStreamReader.ReaderOptions DelimiterStreamReaderOptions { get; set; } = new();

    /// <summary>
    /// Add file name or any other source custom column.
    /// </summary>
    public bool AddInputSourceColumn { get; set; } = true;
}
