using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Utils;

/// <summary>
/// The stream with the cache support. It allows to read data from cache if there was a seek to the initial position.
/// </summary>
internal sealed class CacheStream : Stream
{
    private const int DefaultBufferSize = 4096;

    private readonly Stream _stream;
    private bool _cacheMode = true;

    private readonly DynamicBuffer<byte> _buffer;
    private long _cachePosition;
    private long _streamPosition;
    private bool _isDisposed;

    private long StreamPosition => _stream.CanSeek ? _stream.Position : _streamPosition;

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _stream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => IsInCache ? _cachePosition : StreamPosition;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns <c>true</c> if read from cache instead of source stream.
    /// </summary>
    public bool IsInCache
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _cachePosition < _buffer.Size;
    }

    /// <summary>
    /// Current cache size.
    /// </summary>
    public long CacheSize => _buffer.Size;

    /// <summary>
    /// The underlying (base) stream.
    /// </summary>
    public Stream UnderlyingStream => _stream;

    /// <inheritdoc />
    public CacheStream(Stream stream, int bufferSize = 0)
    {
        _stream = stream;
        _buffer = new(chunkSize: bufferSize > 0 ? bufferSize : DefaultBufferSize);
    }

    /// <inheritdoc />
    public override void Flush()
    {
        _stream.Flush();
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
        if (buffer.IsEmpty)
        {
            return 0;
        }
        // Serve either cache or source, never both: avoids zero-length source reads and uncached gaps.
        if (IsInCache)
        {
            return ReadFromCache(buffer);
        }
        var bytesRead = _stream.Read(buffer);
        OnStreamRead(buffer[..bytesRead]);
        return bytesRead;
    }

    /// <inheritdoc />
    public override int ReadByte()
    {
        Span<byte> value = stackalloc byte[1];
        return Read(value) == 1 ? value[0] : -1;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }
        if (buffer.IsEmpty)
        {
            return ValueTask.FromResult(0);
        }
        if (IsInCache)
        {
            return ValueTask.FromResult(ReadFromCache(buffer.Span));
        }
        return ReadFromStreamAsync(buffer, cancellationToken);
    }

    private async ValueTask<int> ReadFromStreamAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
        OnStreamRead(buffer.Span[..bytesRead]);
        return bytesRead;
    }

    private void OnStreamRead(ReadOnlySpan<byte> data)
    {
        _streamPosition += data.Length;
        if (_cacheMode && !data.IsEmpty)
        {
            _buffer.Write(data);
            _cachePosition = _buffer.Size;
        }
    }

    private int ReadFromCache(Span<byte> buffer)
    {
        var totalCopied = _buffer.CopyTo(buffer, _cachePosition, buffer.Length);
        _cachePosition += totalCopied;
        if (!IsInCache && _stream.CanSeek && _cachePosition != _stream.Position)
        {
            _stream.Position = _cachePosition;
        }
        return (int)totalCopied;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin != SeekOrigin.Begin)
        {
            throw new NotSupportedException("Only SeekOrigin.Begin is supported.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        var cacheSize = _buffer.Size;
        if (offset < cacheSize)
        {
            // Non-seekable source can resume after the cache only if nothing was read past it.
            if (!_stream.CanSeek && _streamPosition != cacheSize)
            {
                throw new NotSupportedException("Data after the cache end was not cached.");
            }
            _cachePosition = offset;
            return offset;
        }

        if (offset != StreamPosition)
        {
            if (!_stream.CanSeek || (_cacheMode && offset != cacheSize))
            {
                throw new NotSupportedException("Cannot seek past the cached data.");
            }
            _stream.Position = offset;
        }
        _cachePosition = cacheSize;
        return offset;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Set not cache mode: do not append to cache anymore and read from the source stream.
    /// </summary>
    public void Freeze()
    {
        _cacheMode = false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        if (disposing)
        {
            _stream.Dispose();
            _buffer.Clear();
        }
        base.Dispose(disposing);
    }
}
