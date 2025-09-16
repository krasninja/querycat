namespace QueryCat.Plugins.Client;

/// <summary>
/// Available Thrift transport types.
/// </summary>
public enum ThriftTransportType
{
    /// <summary>
    /// Named pipes on Windows or local sockets in UNIX.
    /// </summary>
    NamedPipes,

    /// <summary>
    /// TCP sockets.
    /// </summary>
    Tcp,
}
