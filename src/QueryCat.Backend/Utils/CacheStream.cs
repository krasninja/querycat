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

    private readonly DynamicBuffer<byte> _buffer = new(chunkSize: DefaultBufferSize);
    private long _cachePosition;

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
        get => IsInCache ? _cachePosition : _stream.Position;
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
    public CacheStream(Stream stream)
    {
        _stream = stream;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        _stream.Flush();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = 0;

        // Read the cache if we are within it.
        if (IsInCache)
        {
            bytesRead = ReadFromCache(buffer, offset, count);
            if (!IsInCache)
            {
                bytesRead += _stream.Read(buffer, offset + bytesRead, count - bytesRead);
            }
        }
        // Read from the stream.
        else
        {
            bytesRead += _stream.Read(buffer, offset, count);
            WriteToCache(buffer, offset, bytesRead);
        }

        return bytesRead;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        var bytesRead = 0;

        // Read the cache if we are within it.
        if (IsInCache)
        {
            bytesRead = ReadFromCache(buffer, offset, count);
            if (!IsInCache)
            {
                bytesRead += await _stream.ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
            }
        }
        // Read from the stream.
        else
        {
            bytesRead += await _stream.ReadAsync(buffer, offset, count, cancellationToken);
            WriteToCache(buffer, offset, bytesRead);
        }

        return bytesRead;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        var bytesRead = 0;

        // Read the cache if we are within it.
        if (IsInCache)
        {
            bytesRead = ReadFromCache(buffer);
            if (!IsInCache)
            {
                bytesRead += await _stream.ReadAsync(buffer.Slice(bytesRead, buffer.Length - bytesRead), cancellationToken);
            }
        }
        // Read from the stream.
        else
        {
            bytesRead += await _stream.ReadAsync(buffer, cancellationToken);
            WriteToCache(buffer.Slice(0, bytesRead));
        }

        return bytesRead;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteToCache(byte[] buffer, int offset, int bytesRead)
    {
        if (!_cacheMode)
        {
            return 0;
        }
        return WriteToCache(buffer.AsMemory(offset, bytesRead));
    }

    private int WriteToCache(Memory<byte> buffer)
    {
        if (!_cacheMode)
        {
            return 0;
        }
        if (buffer.IsEmpty)
        {
            return 0;
        }
        var span = buffer.Span;
        _buffer.Write(span);
        _cachePosition = _buffer.Size;
        return span.Length;
    }

    private int ReadFromCache(byte[] buffer, int offset, int count)
        => ReadFromCache(buffer.AsMemory(offset, count));

    private int ReadFromCache(Memory<byte> buffer)
    {
        var cachePosition = (int)_cachePosition;
        var span = _buffer.GetSpan(cachePosition, cachePosition + buffer.Length);
        _cachePosition += span.Length;
        span.CopyTo(buffer.Span);
        if (!IsInCache && _cachePosition != _stream.Position)
        {
            _stream.Position = _cachePosition;
        }
        return span.Length;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin != SeekOrigin.Begin)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual((int)origin, (int)SeekOrigin.Begin, nameof(origin));
        }
        if (offset > _stream.Position)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, _stream.Position, nameof(offset));
        }
        _cachePosition = offset;
        return _stream.Position;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
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
        if (disposing)
        {
            _stream.Dispose();
            _buffer.Clear();
        }
        base.Dispose(disposing);
    }
}
