using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace QueryCat.Plugins.Client.Remote;

/// <summary>
/// Utils for <see cref="RemoteObject" />.
/// </summary>
public static class RemoteObjectUtils
{
    public static ValueTask<object?> ToLocalObjectAsync(
        RemoteObject remoteObject,
        ThriftPluginClient pluginClient,
        CancellationToken cancellationToken = default) => ToLocalObjectAsync(
            remoteObject,
            new SimpleThriftSessionProvider(pluginClient.ThriftClient),
            pluginClient.ObjectsStorage,
            pluginClient.Token, cancellationToken);

    public static async ValueTask<object?> ToLocalObjectAsync(
        RemoteObject remoteObject,
        IThriftSessionProvider sessionProvider,
        ObjectsStorage objectsStorage,
        long token = 0,
        CancellationToken cancellationToken = default)
    {
        var type = remoteObject.Type;
        if (type == RemoteObjectType.RowsIterator)
        {
            var iterator = new ThriftRemoteRowsIterator(sessionProvider, remoteObject.Handle, token: token);
            await iterator.InitializeAsync(cancellationToken);
            return iterator;
        }
        if (type == RemoteObjectType.RowsInput)
        {
            return new ThriftRemoteRowsInput(sessionProvider, remoteObject.Handle, remoteObject.Name, token: token);
        }
        if (type == RemoteObjectType.RowsOutput)
        {
            return new ThriftRemoteRowsOutput(sessionProvider, remoteObject.Handle, remoteObject.Name, token: token);
        }
        if (type == RemoteObjectType.Json && !string.IsNullOrEmpty(remoteObject.Data))
        {
            var node = JsonNode.Parse(remoteObject.Data);
            if (node != null)
            {
                return node;
            }
        }
        if (type == RemoteObjectType.Blob)
        {
            return new ThriftRemoteBlobProxy(new RemoteStream(remoteObject.Handle, sessionProvider, token: token));
        }
        if (type == RemoteObjectType.RowsFormatter)
        {
            return new ThriftRemoteRowsFormatter(sessionProvider, objectsStorage, remoteObject.Handle, token: token);
        }
        if (type == RemoteObjectType.AnswerAgent)
        {
            return new ThriftRemoteAnswerAgent(sessionProvider, remoteObject.Handle, token: token);
        }

        return null;
    }
}
