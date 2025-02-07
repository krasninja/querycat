using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class RemoteStream : Stream
{
    private const int BufferSize = 4096 * 2;

    private readonly int _objectHandle;
    private readonly Plugin.Client _client;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => AsyncUtils.RunSync(ct => _client.Blob_GetLengthAsync(_objectHandle, ct));

    /// <inheritdoc />
    public override long Position { get; set; }

    public RemoteStream(int objectHandle, Plugin.Client client)
    {
        _objectHandle = objectHandle;
        _client = client;
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return AsyncUtils.RunSync(ct => ReadAsync(buffer, offset, count, ct));
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytes = await _client.Blob_ReadAsync(_objectHandle, (int)Position, count, cancellationToken);
        Position += bytes.Length;

        var realCount = count;
        if (realCount + offset > bytes.Length)
        {
            realCount = bytes.Length - offset;
        }
        bytes.AsSpan(0, realCount).CopyTo(buffer.AsSpan(offset, count));
        return realCount;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
