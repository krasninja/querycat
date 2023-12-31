using System.Buffers;

namespace QueryCat.Backend.Core.Types.Blob;

internal sealed class EmptyBlobData : IBlobData
{
    public static IBlobData Instance { get; } = new EmptyBlobData();

    /// <inheritdoc />
    public long Length => 0;

    /// <inheritdoc />
    public int GetBytes(byte[] buffer, int offset, int count) => 0;

    /// <inheritdoc />
    public int ApplyAction(ReadOnlySpanAction<byte, object?> action, int offset, object? state = default) => 0;
}
