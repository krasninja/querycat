using System.Threading;
using System.Threading.Tasks;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client.BlobProxy;

public sealed class PluginManagerBlobProxyService : IBlobProxyService
{
    private readonly PluginsManager.IAsync _client;
    private readonly long _token;

    public PluginManagerBlobProxyService(PluginsManager.IAsync client, long token)
    {
        _client = client;
        _token = token;
    }

    /// <inheritdoc />
    public Task<long> Blob_GetLengthAsync(int objectBlobHandle, CancellationToken cancellationToken = default)
    {
        return _client.Blob_GetLengthAsync(_token, objectBlobHandle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<byte[]> Blob_ReadAsync(int objectBlobHandle, int offset, int count, CancellationToken cancellationToken = default)
    {
        return _client.Blob_ReadAsync(_token, objectBlobHandle, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_WriteAsync(int objectBlobHandle, byte[] bytes, CancellationToken cancellationToken = default)
    {
        return _client.Blob_WriteAsync(_token, objectBlobHandle, bytes, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> Blob_GetContentTypeAsync(int objectBlobHandle, CancellationToken cancellationToken = default)
    {
        return _client.Blob_GetContentTypeAsync(_token, objectBlobHandle, cancellationToken);
    }
}
