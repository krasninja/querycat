using System;
using System.IO;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteBlobProxy : IBlobData, IDisposable, IAsyncDisposable
{
    private readonly IThriftSessionProvider _sessionProvider;
    private readonly int _handle;
    private readonly long _token;
    private readonly RemoteStream _remoteStream;
    private volatile bool _isDisposed;

    /// <inheritdoc />
    public string Name => _remoteStream.Name;

    /// <inheritdoc />
    public long Length => _remoteStream.Length;

    /// <inheritdoc />
    public string ContentType => _remoteStream.ContentType;

    public ThriftRemoteBlobProxy(IThriftSessionProvider sessionProvider, int handle, long token)
    {
        _sessionProvider = sessionProvider;
        _handle = handle;
        _token = token;
        _remoteStream = new RemoteStream(handle, sessionProvider, token);
    }

    /// <inheritdoc />
    public Stream GetStream() => new RemoteStream(_handle, _sessionProvider, _token, ownsHandle: false);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        await _remoteStream.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        _remoteStream.Dispose();
    }
}
