using System.IO.Compression;

namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class ZipStreamWrapper : Stream
{
    private readonly Stream _stream;
    private readonly ZipArchive _zip;

    /// <inheritdoc />
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _stream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _stream.CanWrite;

    /// <inheritdoc />
    public override long Length => _stream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public ZipStreamWrapper(Stream stream, ZipArchive zip)
    {
        _stream = stream;
        _zip = zip;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        _stream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _stream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => _stream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _stream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _stream.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _stream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => _stream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _stream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _stream.WriteAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        await _zip.DisposeAsync();
        await base.DisposeAsync();
    }
}
