using System.Diagnostics;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Remote object (can be iterator, BLOB, etc).
/// </summary>
[DebuggerDisplay("Handle = {Handle}, Type = {Type}")]
public sealed class RemoteObject
{
    /// <summary>
    /// Object handle.
    /// </summary>
    public int Handle { get; }

    /// <summary>
    /// Object type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="handle">Handle.</param>
    /// <param name="type">Type.</param>
    public RemoteObject(int handle, string type)
    {
        Handle = handle;
        Type = type;
    }
}
