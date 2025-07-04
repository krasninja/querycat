namespace QueryCat.Backend.Utils;

/// <summary>
/// The extended version of <see cref="MemoryStream" /> that support base stream.
/// </summary>
internal sealed class MemoryFileStream : MemoryStream
{
    /// <summary>
    /// Underlying stream.
    /// </summary>
    public Stream BaseStream { get; }

    public MemoryFileStream(Stream baseStream)
    {
        BaseStream = baseStream;
    }
}
