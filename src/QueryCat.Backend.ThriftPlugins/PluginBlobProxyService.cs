using QueryCat.Plugins.Client.BlobProxy;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class PluginBlobProxyService : IBlobProxyService
{
    private readonly ThriftPluginContext _context;

    public PluginBlobProxyService(ThriftPluginContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<long> Blob_GetLengthAsync(int objectBlobHandle, CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        return await session.ClientProxy.Blob_GetLengthAsync(objectBlobHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> Blob_ReadAsync(int objectBlobHandle, int offset, int count, CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        return await session.ClientProxy.Blob_ReadAsync(objectBlobHandle, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> Blob_WriteAsync(int objectBlobHandle, byte[] bytes, CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        return await session.ClientProxy.Blob_WriteAsync(objectBlobHandle, bytes, cancellationToken);
    }
}
