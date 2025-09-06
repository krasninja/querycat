using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Plugins.Client.Remote;

public sealed class RemoteStream : Stream
{
    private readonly int _objectHandle;
    private readonly IThriftSessionProvider _sessionProvider;
    private readonly long _token;

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
                var session = await _sessionProvider.GetAsync(ct);
                return await session.Client.Blob_GetLengthAsync(_token, _objectHandle, ct);
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
                var session = await _sessionProvider.GetAsync(ct);
                return await session.Client.Blob_GetContentTypeAsync(_token,_objectHandle, ct);
            }) ?? string.Empty;
        }
    }

    /// <summary>
    /// Logical stream name. Optional.
    /// </summary>
    public string Name
    {
        get
        {
            return AsyncUtils.RunSync(async ct =>
            {
                var session = await _sessionProvider.GetAsync(ct);
                return await session.Client.Blob_GetNameAsync(_token,_objectHandle, ct);
            }) ?? string.Empty;
        }
    }

    public RemoteStream(int objectHandle, IThriftSessionProvider sessionProvider, long token = 0)
    {
        _objectHandle = objectHandle;
        _sessionProvider = sessionProvider;
        _token = token;
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
        var session = await _sessionProvider.GetAsync(cancellationToken);
        var bytes = await session.Client.Blob_ReadAsync(_token,_objectHandle, (int)Position, count, cancellationToken);
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

        var session = await _sessionProvider.GetAsync(cancellationToken);
        await session.Client.Blob_WriteAsync(_token, _objectHandle, buffer, cancellationToken);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        AsyncUtils.RunSync(async ct =>
        {
            await DisposeAsync();
        });
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        var session = await _sessionProvider.GetAsync();
        await session.Client.Thread_CloseHandleAsync(_token, _objectHandle);
        await base.DisposeAsync();
    }
}
