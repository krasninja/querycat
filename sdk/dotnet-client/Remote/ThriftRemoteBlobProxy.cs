using System.IO;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteBlobProxy : IBlobData
{
    private readonly RemoteStream _remoteStream;

    /// <inheritdoc />
    public string Name => _remoteStream.Name;

    /// <inheritdoc />
    public long Length => _remoteStream.Length;

    /// <inheritdoc />
    public string ContentType => _remoteStream.ContentType;

    public ThriftRemoteBlobProxy(IThriftSessionProvider sessionProvider, int handle, long token)
    {
        _remoteStream = new RemoteStream(handle, sessionProvider, token);
    }

    public ThriftRemoteBlobProxy(RemoteStream remoteStream)
    {
        _remoteStream = remoteStream;
    }

    /// <inheritdoc />
    public Stream GetStream() => _remoteStream;
}
