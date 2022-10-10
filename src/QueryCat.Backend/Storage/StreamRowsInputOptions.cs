namespace QueryCat.Backend.Storage;

/// <summary>
/// Options for <see cref="StreamRowsInput" />.
/// </summary>
public class StreamRowsInputOptions
{
    /// <summary>
    /// Buffer size.
    /// </summary>
    public const int DefaultBufferSize = 4_096 * 8;

    /// <summary>
    /// Quote character.
    /// </summary>
    public char QuoteChar { get; init; } = '"';

    /// <summary>
    /// Use quote character for parse.
    /// </summary>
    public bool UseQuoteChar { get; init; } = true;

    /// <summary>
    /// Columns delimiters.
    /// </summary>
    public char[] Delimiters { get; init; } = Array.Empty<char>();

    /// <summary>
    /// Buffer size.
    /// </summary>
    public int BufferSize { get; init; } = DefaultBufferSize;

    /// <summary>
    /// Do not take into account empty lines.
    /// </summary>
    public bool SkipEmptyLines { get; init; } = true;
}
