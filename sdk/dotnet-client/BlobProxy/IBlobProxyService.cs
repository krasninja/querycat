using System.Threading;
using System.Threading.Tasks;

namespace QueryCat.Plugins.Client.BlobProxy;

public interface IBlobProxyService
{
    Task<long> Blob_GetLengthAsync(int objectBlobHandle, CancellationToken cancellationToken = default);

    Task<byte[]> Blob_ReadAsync(int objectBlobHandle, int offset, int count, CancellationToken cancellationToken = default);

    Task<long> Blob_WriteAsync(int objectBlobHandle, byte[] bytes, CancellationToken cancellationToken = default);

    Task<string> Blob_GetContentTypeAsync(int objectBlobHandle, CancellationToken cancellationToken = default);
}