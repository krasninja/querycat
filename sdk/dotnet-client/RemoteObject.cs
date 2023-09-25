namespace QueryCat.Plugins.Client;

public sealed class RemoteObject
{
    public int Handle { get; }

    public string Type { get; }

    public RemoteObject(int handle, string type)
    {
        Handle = handle;
        Type = type;
    }
}
