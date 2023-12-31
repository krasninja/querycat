using System.Buffers;
using QueryCat.Backend.Core.Types.Blob;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class RemoteBlobData : IBlobData
{
    private const int BufferSize = 4096 * 2;

    private readonly int _objectHandle;
    private readonly Plugin.Client _client;

    public RemoteBlobData(int objectHandle, Plugin.Client client)
    {
        _objectHandle = objectHandle;
        _client = client;
    }

    /// <inheritdoc />
    public long Length => AsyncUtils.RunSync(ct => _client.Blob_GetLengthAsync(_objectHandle, ct));

    /// <inheritdoc />
    public int GetBytes(byte[] buffer, int offset, int count)
    {
        var bytes = AsyncUtils.RunSync(ct => _client.Blob_ReadAsync(_objectHandle, offset, count, ct));
        if (bytes == null)
        {
            return 0;
        }

        var realCount = count;
        if (realCount + offset > bytes.Length)
        {
            realCount = bytes.Length - offset;
        }
        bytes.AsSpan(offset, realCount).CopyTo(buffer);
        return realCount;
    }

    /// <inheritdoc />
    public int ApplyAction(ReadOnlySpanAction<byte, object?> action, int offset, object? state = default)
    {
        var position = offset;

        int totalBytesRead = 0;
        while (true)
        {
            var bytes = AsyncUtils.RunSync(ct => _client.Blob_ReadAsync(_objectHandle, position, BufferSize, ct));
            if (bytes == null || bytes.Length < 1)
            {
                break;
            }
            action.Invoke(new ReadOnlySpan<byte>(bytes, 0, bytes.Length), state);
            totalBytesRead += bytes.Length;
        }

        return totalBytesRead;
    }
}
