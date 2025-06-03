using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Utils;

/// <summary>
/// The stream with the cache support. It allows to read data from cache if there was a seek to the initial position.
/// </summary>
internal sealed class CacheStream : Stream
{
    private readonly Stream _stream;
    private bool _cacheMode = true;

    private readonly DynamicBuffer<byte> _buffer = new(chunkSize: 1024);
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
    public bool IsInCache => _cachePosition < _buffer.SizeLong;

    /// <summary>
    /// Current cache size.
    /// </summary>
    public long CacheSize => _buffer.SizeLong;

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

        // Read the cache if we are within it..
        if (IsInCache)
        {
            bytesRead = ReadFromCache(buffer, offset, count);
            bytesRead += _stream.Read(buffer, offset + bytesRead, count - bytesRead);
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
        var bytesRead = 0;

        // Read the cache if we are within it..
        if (IsInCache)
        {
            bytesRead = ReadFromCache(buffer, offset, count);
            bytesRead += await _stream.ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
        }
        // Read from the stream.
        else
        {
            bytesRead += await _stream.ReadAsync(buffer, offset, count, cancellationToken);
            WriteToCache(buffer, offset, bytesRead);
        }

        return bytesRead;
    }

    private void WriteToCache(byte[] buffer, int offset, int bytesRead)
    {
        if (bytesRead < 1)
        {
            return;
        }
        var span = buffer.AsSpan(offset, bytesRead);
        if (_cacheMode && !span.IsEmpty)
        {
            _buffer.Write(span);
            _cachePosition = _buffer.SizeLong;
        }
    }

    private int ReadFromCache(byte[] buffer, int offset, int count)
    {
        var cachePosition = (int)_cachePosition;
        var span = _buffer.GetSpan(cachePosition, cachePosition + count);
        _cachePosition += span.Length;
        span.CopyTo(buffer.AsSpan(offset, count));
        if (_cachePosition != _stream.Position)
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
            throw new ArgumentOutOfRangeException(nameof(origin), "Only Begin origin is supported.");
        }
        if (offset > _stream.Position)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek is only supported within cache.");
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
        }
        base.Dispose(disposing);
    }
}
