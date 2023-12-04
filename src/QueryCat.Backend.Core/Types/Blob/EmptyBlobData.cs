using System.Buffers;

namespace QueryCat.Backend.Core.Types.Blob;

internal sealed class EmptyBlobData : IBlobData
{
    public static IBlobData Instance { get; } = new EmptyBlobData();

    /// <inheritdoc />
    public int GetBytes(byte[] buffer, int offset = 0, int count = int.MaxValue) => 0;

    /// <inheritdoc />
    public int ApplyAction<TState>(ReadOnlySpanAction<byte, TState?> action, int offset = 0, TState? state = default) => 0;
}
