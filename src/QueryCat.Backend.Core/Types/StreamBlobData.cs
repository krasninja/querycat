namespace QueryCat.Backend.Core.Types;

public sealed class StreamBlobData : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(() => Stream.Null);

    private readonly Func<Stream> _streamFactory;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public long Length => _streamFactory.Invoke().Length;

    /// <inheritdoc />
    public string ContentType { get; }

    public StreamBlobData(Func<Stream> streamFactory, string? contentType = null, string? name = null)
    {
        _streamFactory = streamFactory;
        ContentType = contentType ?? "application/octet-stream";
        Name = name ?? string.Empty;
    }

    public StreamBlobData(byte[] bytes, string? contentType = null, string? name = null)
        : this(() => new MemoryStream(bytes), contentType, name)
    {
    }

    /// <inheritdoc />
    public Stream GetStream() => _streamFactory.Invoke();
}
