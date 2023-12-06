using System.Buffers;

namespace QueryCat.Backend.Core.Types.Blob;

public class BytesBlobData : IBlobData
{
    private readonly byte[] _bytes;

    public BytesBlobData(byte[] bytes)
    {
        _bytes = bytes;
    }

    /// <inheritdoc />
    public long Length => _bytes.Length;

    /// <inheritdoc />
    public int GetBytes(byte[] buffer, int offset, int count)
    {
        if (offset + count > Length)
        {
            count = (int)Length - offset;
        }
        var span = new ReadOnlySpan<byte>(_bytes, offset, count);
        span.CopyTo(buffer);
        return span.Length;
    }

    /// <inheritdoc />
    public int ApplyAction<TState>(ReadOnlySpanAction<byte, TState?> action, int offset, TState? state = default)
    {
        var span = new ReadOnlySpan<byte>(_bytes, offset, _bytes.Length - offset);
        action.Invoke(span, state);
        return span.Length;
    }
}
