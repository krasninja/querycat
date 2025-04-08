using System.IO;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Client.BlobProxy;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client;

public sealed class RemoteBlobProxy : IBlobData
{
    private readonly RemoteStream _remoteStream;

    /// <inheritdoc />
    public long Length => _remoteStream.Length;

    /// <inheritdoc />
    public string ContentType => _remoteStream.ContentType;

    public RemoteBlobProxy(PluginsManager.IAsync client, int handle, long token)
    {
        _remoteStream = new RemoteStream(handle, new PluginManagerBlobProxyService(client, token));
    }

    public RemoteBlobProxy(RemoteStream remoteStream)
    {
        _remoteStream = remoteStream;
    }

    /// <inheritdoc />
    public Stream GetStream() => _remoteStream;
}
