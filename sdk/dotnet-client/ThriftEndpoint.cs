using System;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Endpoint address information.
/// </summary>
public sealed class ThriftEndpoint
{
    public const string TransportNamedPipes = "net.pipe";
    public const string TransportTcp = "tcp";

    /// <summary>
    /// Host name.
    /// </summary>
    public string Host { get; } = "localhost";

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
    public static ThriftEndpoint Empty { get; } = new(TransportNamedPipes + "://localhost/empty");

    public Uri Uri
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
            return uriBuilder.Uri;
        }
    }

    public ThriftEndpoint(Uri uri)
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
            Port = 445;
        }
        else
        {
            Port = uri.Port;
        }
    }

    public ThriftEndpoint(string uri) : this(new Uri(uri))
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
            Host = host ?? "localhost",
            Path = pipeName
        }.Uri);

    /// <summary>
    /// Create TCP endpoint.
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="host">Host.</param>
    /// <returns>Instance of <see cref="ThriftEndpoint" />.</returns>
    public static ThriftEndpoint CreateTcp(int port, string? host = null)
        => new(new UriBuilder
        {
            Scheme = TransportTcp,
            Host = host ?? "localhost",
            Port = port
        }.Uri);

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
