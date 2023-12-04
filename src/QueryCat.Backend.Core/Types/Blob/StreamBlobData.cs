using System.Buffers;

namespace QueryCat.Backend.Core.Types.Blob;

public class StreamBlobData : IBlobData
{
    private const int BufferSize = 4096;

    private readonly Func<Stream> _streamFactory;

    public StreamBlobData(Func<Stream> streamFactory)
    {
        _streamFactory = streamFactory;
    }

    /// <inheritdoc />
    public int GetBytes(byte[] buffer, int offset = 0, int count = int.MaxValue)
    {
        using var stream = _streamFactory.Invoke();
        stream.Seek(offset, SeekOrigin.Begin);
        if (buffer.Length < count)
        {
            count = buffer.Length;
        }
        return stream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public int ApplyAction<T>(ReadOnlySpanAction<byte, T?> action, int offset = 0, T? state = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        using var stream = _streamFactory.Invoke();
        stream.Seek(offset, SeekOrigin.Begin);

        int bytesRead = 0, totalBytesRead = 0;
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            action.Invoke(new ReadOnlySpan<byte>(buffer, 0, bytesRead), state);
            totalBytesRead += bytesRead;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return totalBytesRead;
    }
}
