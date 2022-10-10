namespace QueryCat.Backend.Relational;

/// <summary>
/// Rows frame options.
/// </summary>
public class RowsFrameOptions
{
    public const int DefaultChunkSize = 4096;

    /// <summary>
    /// Chunk size to grow.
    /// </summary>
    public int ChunkSize { get; internal set; } = DefaultChunkSize;
}
