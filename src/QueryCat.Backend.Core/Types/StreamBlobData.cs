namespace QueryCat.Backend.Core.Types;

public sealed class StreamBlobData : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(() => Stream.Null);

    private readonly Func<Stream> _streamFactory;

    /// <inheritdoc />
    public long Length => _streamFactory.Invoke().Length;

    public StreamBlobData(Func<Stream> streamFactory)
    {
        _streamFactory = streamFactory;
    }

    public StreamBlobData(byte[] bytes) : this(() => new MemoryStream(bytes))
    {
    }

    /// <inheritdoc />
    public Stream GetStream() => _streamFactory.Invoke();
}
