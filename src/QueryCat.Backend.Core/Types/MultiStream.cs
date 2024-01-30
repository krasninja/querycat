namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Aggregates read-only streams, so that classes like StreamReader can treat
/// multiple input streams as a single contiguous block of bytes.
/// </summary>
internal sealed class MultiStream : Stream
{
    // Source: https://github.com/jplane/MultiStream/blob/master/MultiStream.Lib/MultiStream.cs
    private readonly Stream[] _streams;

    private int _index;
    private long _position;

    public MultiStream(params Stream[] streams)
    {
        _streams = streams;
        _index = _streams.Length > 0 ? 0 : -1;
    }

    /// <inheritdoc />
    public override bool CanRead => _streams.All(s => s.CanRead);

    /// <inheritdoc />
    public override bool CanSeek => _streams.All(s => s.CanSeek);

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _streams.Sum(s => s.Length);

    /// <inheritdoc />
    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    /// <inheritdoc />
    public override void Flush()
    {
        var current = 0;

        while (current <= _index)
        {
            _streams[current++].Flush();
        }
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_index == -1)
        {
            return 0;
        }
        var totalBytesRead = 0;

        var bytesRead = _streams[_index].Read(buffer, offset, count);

        totalBytesRead += bytesRead;
        _position += bytesRead;

        while (bytesRead < count)
        {
            if (_index == _streams.Length - 1)
            {
                break;
            }

            offset += bytesRead;
            count -= bytesRead;
            _index++;

            bytesRead = _streams[_index].Read(buffer, offset, count);

            totalBytesRead += bytesRead;
            _position += bytesRead;
        }

        return totalBytesRead;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        var length = Length;

        if (length == 0)
        {
            _position = 0;
            _index = -1;
        }
        else
        {
            var value = 0L;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    value = 0 + offset;
                    break;
                case SeekOrigin.Current:
                    value = _position + offset;
                    break;
                case SeekOrigin.End:
                    value = length + offset;
                    break;
            }

            if (value < 0)
            {
                value = 0;
            }
            else if (value >= length)
            {
                value = length - 1;
            }

            var accum = 0L;

            for (var i = 0; i < _streams.Length; i++)
            {
                var current = _streams[i];
                accum += current.Length;

                if (accum <= value)
                {
                    continue;
                }
                _position = value;
                _index = i;
                _streams[_index].Position = _streams[_index].Length - (accum - value);
                break;
            }
        }

        return _position;
    }

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
