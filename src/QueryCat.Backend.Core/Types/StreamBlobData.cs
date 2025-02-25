namespace QueryCat.Backend.Core.Types;

public sealed class StreamBlobData : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(Stream.Null);

    private readonly Stream _stream;

    /// <inheritdoc />
    public long Length => _stream.Length;

    public StreamBlobData(Stream stream)
    {
        _stream = stream;
    }

    public StreamBlobData(byte[] bytes) : this(new MemoryStream(bytes))
    {
    }

    /// <inheritdoc />
    public Stream GetStream() => _stream;
}
