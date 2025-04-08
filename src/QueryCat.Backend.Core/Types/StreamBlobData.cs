namespace QueryCat.Backend.Core.Types;

public sealed class StreamBlobData : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(() => Stream.Null);

    private readonly Func<Stream> _streamFactory;

    /// <inheritdoc />
    public long Length => _streamFactory.Invoke().Length;

    /// <inheritdoc />
    public string ContentType { get; }

    public StreamBlobData(Func<Stream> streamFactory, string? contentType = null)
    {
        _streamFactory = streamFactory;
        ContentType = contentType ?? "application/octet-stream";
    }

    public StreamBlobData(byte[] bytes) : this(() => new MemoryStream(bytes))
    {
    }

    /// <inheritdoc />
    public Stream GetStream() => _streamFactory.Invoke();
}
