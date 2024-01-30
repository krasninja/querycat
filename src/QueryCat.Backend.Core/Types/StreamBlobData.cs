namespace QueryCat.Backend.Core.Types;

public class StreamBlobData(Func<Stream> streamFactory) : IBlobData
{
    public static IBlobData Empty { get; } = new StreamBlobData(() => Stream.Null);

    /// <inheritdoc />
    public long Length
    {
        get
        {
            using var stream = streamFactory.Invoke();
            return stream.Length;
        }
    }

    public StreamBlobData(byte[] bytes) : this(() => new MemoryStream(bytes))
    {
    }

    /// <inheritdoc />
    public Stream GetStream() => streamFactory.Invoke();
}
