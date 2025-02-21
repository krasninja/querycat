using System;
using Thrift;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Transport utilities.
/// </summary>
public static class ThriftTransportUtils
{
    public const string TransportNamedPipes = "net.pipe";

    /// <summary>
    /// Format RPC URI.
    /// </summary>
    /// <param name="type">Transport type.</param>
    /// <param name="path">Named pipe path.</param>
    /// <param name="host">Host, localhost by default</param>
    /// <returns>URI.</returns>
    public static Uri FormatTransportUri(ThriftTransportType type, string path, string? host = null)
    {
        var scheme = type switch
        {
            ThriftTransportType.NamedPipes => TransportNamedPipes,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        var uriBuilder = new UriBuilder
        {
            Scheme = scheme,
            Host = host ?? "localhost",
            Path = path,
        };
        return uriBuilder.Uri;
    }

    /// <summary>
    /// Create Thrift client transport by specific URI.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <param name="configuration">Transport configuration (or default).</param>
    /// <returns>Instance of <see cref="TTransport" />.</returns>
    public static TTransport CreateClientTransport(Uri uri, TConfiguration? configuration = null)
    {
        // Endpoint format example: net.pipe://localhost/qcat-123.
        configuration ??= new TConfiguration();
        switch (uri.Scheme.ToLower())
        {
            case TransportNamedPipes:
                return new TNamedPipeTransport(uri.Segments[1], configuration);
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
        switch (uri.Scheme.ToLower())
        {
            case TransportNamedPipes:
                var flags = localOnly ? NamedPipeServerFlags.OnlyLocalClients : NamedPipeServerFlags.None;
                return new TNamedPipeServerTransport(uri.Segments[1], configuration, flags, 1);
        }
        throw new ArgumentOutOfRangeException(uri.Scheme, uri, Resources.Errors.NotSupported_Scheme);
    }
}
