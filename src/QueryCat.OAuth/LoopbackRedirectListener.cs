using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QueryCat.OAuth;

/// <summary>
/// Minimal HTTP listener on loopback interface that receives OAuth2 authorization redirect (RFC 8252).
/// </summary>
public sealed class LoopbackRedirectListener : IDisposable
{
    public const string CallbackPath = "/callback";

    private const int MaxRequestSize = 16 * 1024;

    private readonly TcpListener _listener;

    /// <summary>
    /// Redirect URI to use in authorization request.
    /// </summary>
    public string RedirectUri { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="port">Port to listen. Random free port is used if 0.</param>
    public LoopbackRedirectListener(int port = 0)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        var actualPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
        RedirectUri = $"http://127.0.0.1:{actualPort}{CallbackPath}";
    }

    /// <summary>
    /// Wait for the redirect request and return its query parameters.
    /// </summary>
    /// <param name="responseHtmlFactory">Creates HTML page to show in browser from query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query parameters.</returns>
    public async Task<IReadOnlyDictionary<string, string>> WaitForCallbackAsync(
        Func<IReadOnlyDictionary<string, string>, string> responseHtmlFactory,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            await using var stream = client.GetStream();
            var requestLine = await ReadRequestLineAsync(stream, cancellationToken);

            // Example: GET /callback?code=123&state=abc HTTP/1.1.
            var parts = requestLine.Split(' ');
            if (parts.Length < 2 || parts[0] != "GET")
            {
                await WriteResponseAsync(stream, "400 Bad Request", string.Empty, cancellationToken);
                continue;
            }
            var target = parts[1];
            var queryIndex = target.IndexOf('?');
            var path = queryIndex > -1 ? target[..queryIndex] : target;
            if (path != CallbackPath)
            {
                // Browsers can request favicon and other resources.
                await WriteResponseAsync(stream, "404 Not Found", string.Empty, cancellationToken);
                continue;
            }

            var query = ParseQuery(queryIndex > -1 ? target[(queryIndex + 1)..] : string.Empty);
            await WriteResponseAsync(stream, "200 OK", responseHtmlFactory.Invoke(query), cancellationToken);
            return query;
        }
    }

    private static async Task<string> ReadRequestLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxRequestSize];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
            if (read == 0)
            {
                break;
            }
            length += read;
            // Read until the end of headers.
            if (buffer.AsSpan(0, length).IndexOf("\r\n\r\n"u8) > -1)
            {
                break;
            }
        }
        var request = Encoding.ASCII.GetString(buffer, 0, length);
        var lineEnd = request.IndexOf("\r\n", StringComparison.Ordinal);
        return lineEnd > -1 ? request[..lineEnd] : request;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, string status, string html,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(html);
        var headers = $"HTTP/1.1 {status}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(headers), cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    internal static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            var key = separatorIndex > -1 ? pair[..separatorIndex] : pair;
            var value = separatorIndex > -1 ? pair[(separatorIndex + 1)..] : string.Empty;
            result[Unescape(key)] = Unescape(value);
        }
        return result;

        static string Unescape(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listener.Stop();
        _listener.Dispose();
    }
}
