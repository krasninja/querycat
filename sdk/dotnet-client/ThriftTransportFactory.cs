using System;
using Thrift;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Transport utilities.
/// </summary>
public static class ThriftTransportFactory
{
    /// <summary>
    /// Create Thrift client transport by specific URI.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <param name="configuration">Transport configuration (or default).</param>
    /// <returns>Instance of <see cref="TTransport" />.</returns>
    public static TTransport CreateClientTransport(Uri uri, TConfiguration? configuration = null)
    {
        configuration ??= new TConfiguration();
        switch (uri.Scheme.ToLower())
        {
            case ThriftEndpoint.TransportNamedPipes:
                // Endpoint format example: net.pipe://localhost/qcat-123.
                return new TNamedPipeTransport(uri.Segments[1], configuration);
            case ThriftEndpoint.TransportTcp:
                // Endpoint format example: tcp://localhost:6780.
                return new TSocketTransport(uri.Host, uri.Port, configuration);
        }
        throw new ArgumentOutOfRangeException(uri.Scheme, uri, Resources.Errors.NotSupported_Scheme);
    }

    /// <summary>
    /// Create Thrift server transport by specific URI.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <param name="configuration">Transport configuration (or default).</param>
    /// <param name="localOnly">Accept only local connections.</param>
    /// <returns>Instance of <see cref="TServerTransport" />.</returns>
    public static TServerTransport CreateServerTransport(Uri uri, TConfiguration? configuration = null,
        bool localOnly = true)
    {
        // Endpoint format example: net.pipe://localhost/qcat-123.
        configuration ??= new TConfiguration();
        var flags = localOnly ? NamedPipeServerFlags.OnlyLocalClients : NamedPipeServerFlags.None;
        switch (uri.Scheme.ToLower())
        {
            case ThriftEndpoint.TransportNamedPipes:
                return new TNamedPipeServerTransport(uri.Segments[1], configuration, flags, 1);
            case ThriftEndpoint.TransportTcp:
                return new TServerSocketTransport(uri.Port, new TConfiguration());
        }
        throw new ArgumentOutOfRangeException(uri.Scheme, uri, Resources.Errors.NotSupported_Scheme);
    }
}
