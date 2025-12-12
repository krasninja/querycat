using System;
using Thrift.Protocol;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Endpoint address information.
/// </summary>
public sealed class ThriftEndpoint
{
    public const string TransportNamedPipes = "net.pipe";
    public const string TransportTcp = "tcp";

    private const int NamedPipesPort = 445;

    private static string LoopbackHost { get; } = System.Net.IPAddress.Loopback.ToString();

    /// <summary>
    /// Host name.
    /// </summary>
    public string Host { get; } = LoopbackHost;

    /// <summary>
    /// Transport type.
    /// </summary>
    public ThriftTransportType TransportType { get; }

    /// <summary>
    /// Named pipe name. Only available for named pipes transport.
    /// </summary>
    public string NamedPipe { get; } = string.Empty;

    /// <summary>
    /// TCP port
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Empty instance of <see cref="ThriftEndpoint" />.
    /// </summary>
    public static ThriftEndpoint Empty { get; } = new(TransportNamedPipes + $"://{LoopbackHost}/empty");

    /// <summary>
    /// Endpoint URI.
    /// </summary>
    public SimpleUri Uri
    {
        get
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = ConvertTransportType(TransportType),
            };
            if (!string.IsNullOrEmpty(Host))
            {
                uriBuilder.Host = Host;
            }
            if (TransportType == ThriftTransportType.NamedPipes)
            {
                uriBuilder.Path = NamedPipe;
            }
            else
            {
                uriBuilder.Port = Port;
            }
            return new SimpleUri(uriBuilder.ToString());
        }
    }

    public ThriftEndpoint(SimpleUri uri)
    {
        TransportType = uri.Scheme switch
        {
            TransportNamedPipes => ThriftTransportType.NamedPipes,
            TransportTcp => ThriftTransportType.Tcp,
            _ => throw new ArgumentOutOfRangeException(nameof(uri.Scheme), uri.Scheme, null),
        };
        try
        {
            Host = uri.Host;
        }
        catch (UriFormatException)
        {
        }
        if (TransportType == ThriftTransportType.NamedPipes)
        {
            NamedPipe = uri.Segments[1];
            Port = NamedPipesPort;
        }
        else
        {
            Port = uri.Port;
        }
    }

    public ThriftEndpoint(string uri) : this(new SimpleUri(uri))
    {
    }

    /// <summary>
    /// Create named pipes endpoint.
    /// </summary>
    /// <param name="pipeName">Pipe name.</param>
    /// <param name="host">Host.</param>
    /// <returns>Instance of <see cref="ThriftEndpoint" />.</returns>
    public static ThriftEndpoint CreateNamedPipe(string pipeName, string? host = null)
        => new(new UriBuilder
        {
            Scheme = TransportNamedPipes,
            Host = host ?? LoopbackHost,
            Path = pipeName
        }.ToString());

    /// <summary>
    /// Create TCP endpoint.
    /// </summary>
    /// <param name="port">Port number (or random).</param>
    /// <param name="host">Host.</param>
    /// <returns>Instance of <see cref="ThriftEndpoint" />.</returns>
    public static ThriftEndpoint CreateTcp(int port = 0, string? host = null)
        => new(new UriBuilder
        {
            Scheme = TransportTcp,
            Host = host ?? LoopbackHost,
            Port = port
        }.ToString());

    private static readonly char[] IdentifierCharacters =
    [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L',
        'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    ];

    /// <summary>
    /// Generate random letters string.
    /// </summary>
    /// <param name="prefix">Prefix.</param>
    /// <param name="length">String length.</param>
    /// <returns>Random length string.</returns>
    public static string GenerateIdentifier(string? prefix = null, int length = 12)
    {
        var randomChars = string.Join(
            string.Empty,
            Random.Shared.GetItems(IdentifierCharacters, length));
        if (string.IsNullOrEmpty(prefix))
        {
            return prefix + '-' +  randomChars;
        }
        return randomChars;
    }

    private static string ConvertTransportType(ThriftTransportType type)
        => type switch
        {
            ThriftTransportType.NamedPipes => TransportNamedPipes,
            ThriftTransportType.Tcp => TransportTcp,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    /// <inheritdoc />
    public override string ToString() => Uri.ToString();
}
