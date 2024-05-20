using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Options for <see cref="StreamRowsInput" />.
/// </summary>
public class StreamRowsInputOptions
{
    /// <summary>
    /// Quote character.
    /// </summary>
    public DelimiterStreamReader.ReaderOptions DelimiterStreamReaderOptions { get; set; } = new()
    {
        Culture = Application.Culture,
    };

    /// <summary>
    /// Add file name or any other source custom column.
    /// </summary>
    public bool AddInputSourceColumn { get; set; } = true;

    /// <summary>
    /// Unique cache key. If specified it will be used for persistent cache.
    /// </summary>
    public string[] CacheKeys { get; set; } = [];
}
