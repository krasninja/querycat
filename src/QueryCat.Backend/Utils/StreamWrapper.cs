namespace QueryCat.Backend.Utils;

/// <summary>
/// Wraps stream. Provides the ability to have its own position, so several wrappers
/// can read the same seekable stream independently. For non-seekable streams the position
/// is the number of bytes read or written through the wrapper.
/// The inner stream is disposed together with the wrapper unless <c>leaveOpen</c> is set.
/// The class is not thread-safe, wrappers over the same stream must not be used concurrently.
/// </summary>
internal sealed class StreamWrapper : Stream
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private long _position;
    private bool _isDisposed;

    /// <inheritdoc />
    public override bool CanRead => !_isDisposed && _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => !_isDisposed && _stream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => !_isDisposed && _stream.CanWrite;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _stream.Length;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _position;
        }

        set
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            if (!_stream.CanSeek)
            {
                throw new NotSupportedException();
            }
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _position = value;
        }
    }

    public StreamWrapper(Stream stream, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _position = stream.CanSeek ? stream.Position : 0;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _stream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        SyncPosition();
        var bytesRead = _stream.Read(buffer);
        _position += bytesRead;
        return bytesRead;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        SyncPosition();
        var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
        _position += bytesRead;
        return bytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!_stream.CanSeek)
        {
            throw new NotSupportedException();
        }

        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _stream.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (newPosition < 0)
        {
            throw new IOException();
        }
        _position = newPosition;
        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _stream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        SyncPosition();
        _stream.Write(buffer);
        _position += buffer.Length;
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        SyncPosition();
        await _stream.WriteAsync(buffer, cancellationToken);
        _position += buffer.Length;
    }

    /// <summary>
    /// Move the inner stream to the wrapper position, it may have been moved by other readers.
    /// </summary>
    private void SyncPosition()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_stream.CanSeek && _stream.Position != _position)
        {
            _stream.Position = _position;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            if (disposing && !_leaveOpen)
            {
                _stream.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            if (!_leaveOpen)
            {
                await _stream.DisposeAsync();
            }
        }
        await base.DisposeAsync();
    }
}
