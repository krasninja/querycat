using System.Text.Json.Nodes;

namespace QueryCat.Plugins.Client.Remote;

/// <summary>
/// Extensions for <see cref="RemoteObject" />.
/// </summary>
public static class RemoteObjectExtensions
{
    public static object? ToLocalObject(
        this RemoteObject remoteObject,
        ThriftPluginClient pluginClient) => ToLocalObject(
            remoteObject,
            new SimpleThriftSessionProvider(pluginClient.ThriftClient),
            pluginClient.ObjectsStorage,
            pluginClient.Token);

    public static object? ToLocalObject(
        this RemoteObject remoteObject,
        IThriftSessionProvider sessionProvider,
        ObjectsStorage objectsStorage,
        long token = 0)
    {
        var type = remoteObject.Type;
        if (type == RemoteObjectType.RowsInput || type == RemoteObjectType.RowsIterator)
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
