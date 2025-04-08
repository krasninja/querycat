using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client.BlobProxy;

namespace QueryCat.Plugins.Client;

public sealed class RemoteStream : Stream
{
    private const int BufferSize = 4096 * 2;

    private readonly int _objectHandle;
    private readonly IBlobProxyService _proxy;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            return AsyncUtils.RunSync(async ct =>
            {
                return await _proxy.Blob_GetLengthAsync(_objectHandle, ct);
            });
        }
    }

    /// <inheritdoc />
    public override long Position { get; set; }

    /// <summary>
    /// MIME content type. Optional.
    /// </summary>
    public string ContentType
    {
        get
        {
            return AsyncUtils.RunSync(async ct =>
            {
                return await _proxy.Blob_GetContentTypeAsync(_objectHandle, ct);
            }) ?? string.Empty;
        }
    }

    public RemoteStream(int objectHandle, IBlobProxyService proxy)
    {
        _objectHandle = objectHandle;
        _proxy = proxy;
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
        var bytes = await _proxy.Blob_ReadAsync(_objectHandle, (int)Position, count, cancellationToken);
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

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (offset > 0 || buffer.Length > count)
        {
            buffer = buffer.AsSpan(offset, count).ToArray();
        }

        await _proxy.Blob_WriteAsync(_objectHandle, buffer, cancellationToken);
    }
}
