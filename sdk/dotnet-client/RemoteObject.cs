namespace QueryCat.Plugins.Client;

/// <summary>
/// Remote object (can be iterator, BLOB, etc).
/// </summary>
public sealed class RemoteObject
{
    /// <summary>
    /// Object handle.
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// Object type.
    /// </summary>
    public RemoteObjectType Type { get; }

    /// <summary>
    /// Object type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Object raw data (for JSON type).
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="handle">Handle.</param>
    /// <param name="type">Remote object type.</param>
    /// <param name="name">Object name..</param>
    public RemoteObject(int handle, RemoteObjectType type, string name)
    {
        Handle = handle;
        Type = type;
        Name = name;
    }

    /// <inheritdoc />
    public override string ToString() => $"Type = {Type}, Handle = {Handle}";
}
