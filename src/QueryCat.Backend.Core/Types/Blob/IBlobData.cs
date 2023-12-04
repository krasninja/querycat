using System.Buffers;

namespace QueryCat.Backend.Core.Types.Blob;

/// <summary>
/// BLOB data provider.
/// </summary>
public interface IBlobData
{
    /// <summary>
    /// Reads a sequence of bytes from the current stream.
    /// </summary>
    /// <param name="buffer">Target array of bytes.</param>
    /// <param name="offset">Offset it BLOB.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The total number of bytes read.</returns>
    int GetBytes(byte[] buffer, int offset = 0, int count = int.MaxValue);

    /// <summary>
    /// Apply delegate to BLOB data.
    /// </summary>
    /// <param name="action">Action to apply.</param>
    /// <param name="offset">Offset within the BLOB.</param>
    /// <param name="state">State to be passed to delegate.</param>
    /// <returns>The total number of bytes read.</returns>
    int ApplyAction<TState>(ReadOnlySpanAction<byte, TState?> action, int offset = 0, TState? state = default);
}
