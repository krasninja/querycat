namespace QueryCat.Backend.Core.Types;

public sealed class StreamBlobData : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(() => Stream.Null);

    private readonly Func<Stream> _streamFactory;
    private readonly long? _length;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public long Length
    {
        get
        {
            if (_length.HasValue)
            {
                return _length.Value;
            }
            using var stream = _streamFactory.Invoke();
            return stream.CanSeek ? stream.Length : -1;
        }
    }

    /// <inheritdoc />
    public string ContentType { get; }

    public StreamBlobData(Func<Stream> streamFactory, string? contentType = null, string? name = null, long? length = null)
    {
        _streamFactory = streamFactory;
        _length = length;
        ContentType = contentType ?? "application/octet-stream";
        Name = name ?? string.Empty;
    }

    public StreamBlobData(byte[] bytes, string? contentType = null, string? name = null)
        : this(() => new MemoryStream(bytes), contentType, name)
    {
        _length = bytes.Length;
    }

    /// <inheritdoc />
    public Stream GetStream() => _streamFactory.Invoke();
}
