using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// MCP transport over HTTP. Every JSON-RPC message is sent by the client as a separate POST request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/basic/transports#streamable-http.
/// </remarks>
public sealed class McpWebTransport : IMcpTransport, IAsyncDisposable
{
    private const string DefaultEndpointUri = "http://localhost:6799/";
    private const long MaxRequestLength = 1024 * 1024;

    private HttpListener? _httpListener;
    private readonly string? _allowOrigin;
    private readonly ConcurrentDictionary<Guid, HttpListenerContext> _contexts = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpWebTransport));

    /// <summary>
    /// Endpoint URI.
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Server options.</param>
    public McpWebTransport(McpServerOptions options)
    {
        Uri = options.Url ?? DefaultEndpointUri;
        _allowOrigin = options.AllowOrigin;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_httpListener != null)
        {
            return Task.CompletedTask;
        }
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(Uri);
        _httpListener.IgnoreWriteExceptions = true;
        _httpListener.Start();
        Console.Out.WriteLine(Resources.Messages.McpServerListen, Uri);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _httpListener?.Close();
        _httpListener = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<JsonRpcRequest?> GetNextRequestAsync(CancellationToken cancellationToken = default)
    {
        var httpListener = EnsureStarted();

        // HttpListener.GetContextAsync does not support cancellation, stop the listener instead.
        await using var registration = cancellationToken.Register(() => httpListener.Stop());
        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = await httpListener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException
                                      && cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }

            try
            {
                var request = await GetRequestAsync(context, cancellationToken).ConfigureAwait(false);
                if (request != null)
                {
                    _contexts[request.SpanId] = context;
                    return request;
                }
            }
            catch (JsonException e)
            {
                await SendResponseInternalAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeParseError, e.Message)),
                    context,
                    HttpStatusCode.BadRequest,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogDebug(e, "[{Address}]: cannot process request.", context.Request.RemoteEndPoint.Address);
                await SendResponseInternalAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeInternalError, e.Message)),
                    context,
                    HttpStatusCode.InternalServerError,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task SendResponseAsync(JsonRpcFrame response, CancellationToken cancellationToken = default)
    {
        _ = EnsureStarted();
        if (!_contexts.TryRemove(response.SpanId, out var context))
        {
            _logger.LogWarning("Incorrect request span id {SpanId}.", response.SpanId);
            return;
        }

        await SendResponseInternalAsync(response, context, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SendResponseInternalAsync(
        JsonRpcFrame response,
        HttpListenerContext context,
        HttpStatusCode statusCode,
        CancellationToken cancellationToken)
    {
        context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;
        context.Response.StatusCode = (int)statusCode;

        if (response is JsonRpcResponse successResponse)
        {
            if (successResponse.Result != null)
            {
                await JsonSerializer.SerializeAsync(context.Response.OutputStream, successResponse,
                    SourceGenerationContext.Default.JsonRpcResponse, cancellationToken);
            }
            else
            {
                // Acknowledgement of a notification.
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            }
        }
        else if (response is JsonRpcErrorResponse errorResponse)
        {
            await JsonSerializer.SerializeAsync(context.Response.OutputStream, errorResponse,
                SourceGenerationContext.Default.JsonRpcErrorResponse, cancellationToken);
        }

        context.Response.Close();
    }

    private HttpListener EnsureStarted()
    {
        if (_httpListener == null)
        {
            throw new InvalidOperationException("The server has not been started.");
        }
        return _httpListener;
    }

    private async Task<JsonRpcRequest?> GetRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        response.Headers["Server"] = Application.GetProductFullName();

        if (context.Request.HttpMethod != HttpMethod.Post.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.Close();
            return null;
        }

        // DNS rebinding protection.
        if (!IsOriginAllowed(context.Request.Headers["Origin"]))
        {
            _logger.LogWarning("[{Address}]: origin is not allowed: {Origin}.",
                context.Request.RemoteEndPoint.Address, context.Request.Headers["Origin"]);
            response.StatusCode = (int)HttpStatusCode.Forbidden;
            response.Close();
            return null;
        }

        // Large request protection.
        if (context.Request.ContentLength64 > MaxRequestLength)
        {
            _logger.LogWarning("[{Address}]: request content length is too large: {Length} bytes.",
                context.Request.RemoteEndPoint.Address, context.Request.ContentLength64);
            response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
            response.Close();
            return null;
        }

        var request = await JsonSerializer.DeserializeAsync(context.Request.InputStream,
            SourceGenerationContext.Default.JsonRpcRequest, cancellationToken);
        if (request == null)
        {
            _logger.LogWarning("[{Address}]: request is null.", context.Request.RemoteEndPoint.Address);
            await SendResponseInternalAsync(
                new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeInvalidRequest,
                    Resources.Messages.McpInvalidRequest)),
                context,
                HttpStatusCode.BadRequest,
                cancellationToken);
            return null;
        }

        request.SpanId = Guid.NewGuid();
        return request;
    }

    private bool IsOriginAllowed(string? origin)
    {
        // Non-browser clients do not send the header.
        if (string.IsNullOrEmpty(origin) || _allowOrigin == "*")
        {
            return true;
        }
        if (!string.IsNullOrEmpty(_allowOrigin)
            && origin.TrimEnd('/').Equals(_allowOrigin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return System.Uri.TryCreate(origin, UriKind.Absolute, out var originUri) && originUri.IsLoopback;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _httpListener?.Close();
        _httpListener = null;
        return ValueTask.CompletedTask;
    }
}
