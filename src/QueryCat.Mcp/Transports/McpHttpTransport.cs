using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp.Transports;

/// <summary>
/// MCP transport over HTTP. Every JSON-RPC message is sent by the client as a separate POST request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http.
/// </remarks>
public sealed class McpHttpTransport : IMcpTransport, IAsyncDisposable
{
    private const string DefaultEndpointUri = "http://localhost:6799/mcp/";
    private const long MaxRequestLength = 1024 * 1024;

    private const string HeaderProtocolVersion = "MCP-Protocol-Version";
    private const string HeaderMethod = "Mcp-Method";
    private const string HeaderName = "Mcp-Name";
    private const string Base64Prefix = "=?base64?";
    private const string Base64Suffix = "?=";

    private static readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(30);
    private static readonly byte[] _keepAlivePayload = ":\n\n"u8.ToArray();

    private HttpListener? _httpListener;
    private CancellationTokenSource? _keepAliveCancellationTokenSource;
    private readonly string? _allowOrigin;
    private readonly ConcurrentDictionary<Guid, PendingRequest> _contexts = new();
    private readonly ConcurrentDictionary<Guid, SseStream> _streams = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpHttpTransport));

    /// <summary>
    /// Endpoint URI.
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Server options.</param>
    public McpHttpTransport(McpServerOptions options)
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
        _keepAliveCancellationTokenSource = new CancellationTokenSource();
        _ = KeepAliveLoopAsync(_keepAliveCancellationTokenSource.Token);
        Console.Out.WriteLine(Resources.Messages.McpServerListen, Uri);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Close();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<JsonRpcRequest?> GetNextRequestAsync(CancellationToken cancellationToken = default)
    {
        var httpListener = EnsureStarted();

        // HttpListener.GetContextAsync does not support cancellation, stop the listener instead.
        await using var registration = cancellationToken.Register(httpListener.Stop);
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
                    _contexts[request.SpanId] = new PendingRequest(context, request.IsLegacy == true);
                    return request;
                }
            }
            catch (JsonException e)
            {
                await SendResponseInternalAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeParseError, e.Message)),
                    context,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "[{Address}]: cannot process request.", context.Request.RemoteEndPoint.Address);
                await SendResponseInternalAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeInternalError,
                        Resources.Errors.McpInternalError)),
                    context,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task SendResponseAsync(JsonRpcFrame response, CancellationToken cancellationToken = default)
    {
        _ = EnsureStarted();

        // The request response is already streamed (SSE).
        if (_streams.TryGetValue(response.SpanId, out var stream))
        {
            if (IsEmptyResponse(response))
            {
                // Long-lived request (subscription), keep the stream open.
                return;
            }
            await WriteSseMessageAsync(response.SpanId, stream, SerializeFrame(response), cancellationToken).ConfigureAwait(false);
            RemoveStream(response.SpanId);
            return;
        }

        if (!_contexts.TryRemove(response.SpanId, out var pendingRequest))
        {
            _logger.LogDebug("Request {SpanId} is already completed.", response.SpanId);
            return;
        }

        // Legacy clients treat non-success HTTP status as a transport error, JSON-RPC errors are returned with 200.
        await SendResponseInternalAsync(response, pendingRequest.Context, cancellationToken,
            pendingRequest.IsLegacy && response is JsonRpcErrorResponse ? HttpStatusCode.OK : null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SendNotificationAsync(JsonRpcNotification notification, Guid spanId, CancellationToken cancellationToken = default)
    {
        _ = EnsureStarted();
        var json = JsonSerializer.Serialize(notification, SourceGenerationContext.Default.JsonRpcNotification);

        // Unsolicited notification for the legacy clients, send to all standalone (GET) streams.
        if (spanId == Guid.Empty)
        {
            foreach (var (legacySpanId, legacyStream) in _streams)
            {
                if (legacyStream.IsStandalone)
                {
                    await WriteSseMessageAsync(legacySpanId, legacyStream, json, cancellationToken).ConfigureAwait(false);
                }
            }
            return true;
        }

        if (!_streams.TryGetValue(spanId, out var stream))
        {
            if (!_contexts.TryRemove(spanId, out var pendingRequest))
            {
                _logger.LogDebug("Request {SpanId} is already completed, skip '{Method}' notification.",
                    spanId, notification.Method);
                return false;
            }
            stream = OpenSseStream(spanId, pendingRequest.Context);
        }

        return await WriteSseMessageAsync(spanId, stream, json, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsEmptyResponse(JsonRpcFrame response) => response is JsonRpcResponse { Result: null };

    private static string SerializeFrame(JsonRpcFrame response)
    {
        if (response is JsonRpcErrorResponse errorResponse)
        {
            return JsonSerializer.Serialize(errorResponse, SourceGenerationContext.Default.JsonRpcErrorResponse);
        }
        return JsonSerializer.Serialize((JsonRpcResponse)response, SourceGenerationContext.Default.JsonRpcResponse);
    }

    private static async Task SendResponseInternalAsync(
        JsonRpcFrame response,
        HttpListenerContext context,
        CancellationToken cancellationToken,
        HttpStatusCode? statusCode = null)
    {
        if (IsEmptyResponse(response))
        {
            // Acknowledgement of a notification.
            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            context.Response.Close();
            return;
        }

        context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        if (response is JsonRpcErrorResponse errorResponse)
        {
            context.Response.StatusCode = (int)(statusCode ?? GetErrorStatusCode(errorResponse.Error.Code));
        }
        var payload = Encoding.UTF8.GetBytes(SerializeFrame(response));
        await context.Response.OutputStream.WriteAsync(payload, cancellationToken);
        context.Response.Close();
    }

    private static HttpStatusCode GetErrorStatusCode(int errorCode) => errorCode switch
    {
        McpErrorModel.ErrorCodeMethodNotFound => HttpStatusCode.NotFound,
        McpErrorModel.ErrorCodeInternalError => HttpStatusCode.InternalServerError,
        McpErrorModel.ErrorCodeParseError
            or McpErrorModel.ErrorCodeInvalidRequest
            or McpErrorModel.ErrorCodeInvalidParams
            or McpErrorModel.ErrorCodeHeaderMismatch
            or McpErrorModel.ErrorCodeMissingRequiredClientCapability
            or McpErrorModel.ErrorCodeUnsupportedProtocolVersion => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.OK,
    };

    private HttpListener EnsureStarted()
    {
        if (_httpListener == null)
        {
            throw new InvalidOperationException("The server has not been started.");
        }
        return _httpListener;
    }

    private SseStream OpenSseStream(Guid spanId, HttpListenerContext context, bool isStandalone = false)
    {
        var response = context.Response;
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
        response.SendChunked = true;

        var stream = new SseStream(context, isStandalone);
        _streams[spanId] = stream;
        _logger.LogDebug("[{Address}]: response stream {SpanId} is open.",
            context.Request.RemoteEndPoint.Address, spanId);
        return stream;
    }

    private async Task<bool> WriteSseMessageAsync(Guid spanId, SseStream stream, string json, CancellationToken cancellationToken)
        => await WriteSseAsync(spanId, stream, Encoding.UTF8.GetBytes($"event: message\ndata: {json}\n\n"), cancellationToken)
            .ConfigureAwait(false);

    private async Task<bool> WriteSseAsync(Guid spanId, SseStream stream, byte[] payload, CancellationToken cancellationToken)
    {
        await stream.WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await stream.Context.Response.OutputStream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await stream.Context.Response.OutputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or IOException or InvalidOperationException)
        {
            // The client disconnected, drop the stream.
            _logger.LogDebug(e, "Response stream {SpanId} is closed.", spanId);
        }
        finally
        {
            stream.WriteLock.Release();
        }

        RemoveStream(spanId);
        return false;
    }

    private async Task KeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        // Keep long-lived streams from being closed by intermediaries and detect disconnected clients.
        using var timer = new PeriodicTimer(_keepAliveInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var (spanId, stream) in _streams)
                {
                    await WriteSseAsync(spanId, stream, _keepAlivePayload, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RemoveStream(Guid spanId)
    {
        if (_streams.TryRemove(spanId, out var stream))
        {
            stream.Close();
        }
    }

    private void Close()
    {
        _keepAliveCancellationTokenSource?.Cancel();
        _keepAliveCancellationTokenSource?.Dispose();
        _keepAliveCancellationTokenSource = null;
        foreach (var (spanId, _) in _streams)
        {
            RemoveStream(spanId);
        }
        _httpListener?.Close();
        _httpListener = null;
    }

    /// <summary>
    /// The request that is waiting for the response.
    /// </summary>
    /// <param name="Context">HTTP context.</param>
    /// <param name="IsLegacy">The request uses a legacy protocol version.</param>
    private sealed record PendingRequest(HttpListenerContext Context, bool IsLegacy);

    /// <summary>
    /// The request response that is streamed as server-sent events.
    /// </summary>
    private sealed class SseStream
    {
        public HttpListenerContext Context { get; }

        /// <summary>
        /// The standalone stream for server-initiated messages opened by GET request (legacy protocol versions).
        /// </summary>
        public bool IsStandalone { get; }

        public SemaphoreSlim WriteLock { get; } = new(1, 1);

        public SseStream(HttpListenerContext context, bool isStandalone)
        {
            Context = context;
            IsStandalone = isStandalone;
        }

        public void Close()
        {
            try
            {
                Context.Response.Close();
            }
            catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                // The client may already be disconnected.
            }
        }
    }

    private async Task<JsonRpcRequest?> GetRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        response.Headers["Server"] = Application.GetProductFullNameForWeb();

        // GET (standalone stream) is supported only for legacy protocol versions,
        // DELETE (session termination) is not supported since the server does not use sessions.
        var isStandaloneStreamRequest = IsStandaloneStreamRequest(context.Request);
        if (context.Request.HttpMethod != HttpMethod.Post.Method && !isStandaloneStreamRequest)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.Headers["Allow"] = HttpMethod.Post.Method;
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

        if (isStandaloneStreamRequest)
        {
            await OpenStandaloneStreamAsync(context, cancellationToken);
            return null;
        }

        // Large request protection. Content length is not set for chunked requests, so limit the read as well.
        if (context.Request.ContentLength64 > MaxRequestLength)
        {
            _logger.LogWarning("[{Address}]: request content length is too large.", context.Request.RemoteEndPoint.Address);
            await SendResponseInternalAsync(
                new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeInvalidRequest,
                    Resources.Errors.McpRequestTooLarge)),
                context,
                cancellationToken,
                HttpStatusCode.RequestEntityTooLarge);
            return null;
        }

        var request = await JsonSerializer.DeserializeAsync(context.Request.InputStream,
            SourceGenerationContext.Default.JsonRpcRequest, cancellationToken);
        if (request == null)
        {
            _logger.LogWarning("[{Address}]: request is null.", context.Request.RemoteEndPoint.Address);
            await SendResponseInternalAsync(
                new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeInvalidRequest,
                    Resources.Errors.McpInvalidRequest)),
                context,
                cancellationToken);
            return null;
        }

        // Legacy request: the initialization handshake, or the request without the per-request protocol version
        // and with legacy protocol version header (or without it).
        var protocolVersionHeader = context.Request.Headers[HeaderProtocolVersion];
        request.IsLegacy = request.GetProtocolVersion() == null
            && (request.Method == "initialize"
                || protocolVersionHeader == null
                || McpServer.IsLegacyProtocolVersion(protocolVersionHeader));

        // Header requirements for notifications are not defined. Legacy protocol versions do not require them.
        if (!request.IsNotification && !request.IsLegacy.Value)
        {
            var headerError = ValidateHeaders(context.Request, request);
            if (headerError != null)
            {
                _logger.LogDebug("[{Address}]: {Error}", context.Request.RemoteEndPoint.Address, headerError);
                await SendResponseInternalAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeHeaderMismatch, headerError))
                    {
                        Id = request.Id,
                    },
                    context,
                    cancellationToken);
                return null;
            }
        }

        request.SpanId = Guid.NewGuid();
        return request;
    }

    private static bool IsStandaloneStreamRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod != HttpMethod.Get.Method
            || !(request.Headers["Accept"]?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return false;
        }
        var protocolVersion = request.Headers[HeaderProtocolVersion];
        return protocolVersion == null || McpServer.IsLegacyProtocolVersion(protocolVersion);
    }

    private async Task OpenStandaloneStreamAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var spanId = Guid.NewGuid();
        var stream = OpenSseStream(spanId, context, isStandalone: true);

        // Send a comment line to flush the headers and establish the stream.
        await WriteSseAsync(spanId, stream, _keepAlivePayload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate that the standard MCP request headers match the request body.
    /// </summary>
    /// <param name="httpRequest">HTTP request.</param>
    /// <param name="request">JSON-RPC request.</param>
    /// <returns>Error message or <c>null</c> if the headers are valid.</returns>
    private static string? ValidateHeaders(HttpListenerRequest httpRequest, JsonRpcRequest request)
    {
        // Protocol version. If the body does not contain the version, the request is rejected by the server.
        var protocolVersion = httpRequest.Headers[HeaderProtocolVersion];
        if (protocolVersion == null)
        {
            return string.Format(Resources.Errors.McpHeaderMissing, HeaderProtocolVersion);
        }
        var bodyProtocolVersion = request.GetProtocolVersion();
        if (bodyProtocolVersion != null && protocolVersion != bodyProtocolVersion)
        {
            return string.Format(Resources.Errors.McpHeaderMismatch, HeaderProtocolVersion, protocolVersion, bodyProtocolVersion);
        }

        // Method.
        var method = httpRequest.Headers[HeaderMethod];
        if (method == null)
        {
            return string.Format(Resources.Errors.McpHeaderMissing, HeaderMethod);
        }
        if (method != request.Method)
        {
            return string.Format(Resources.Errors.McpHeaderMismatch, HeaderMethod, method, request.Method);
        }

        // Name.
        var bodyNameParam = request.Method switch
        {
            "tools/call" or "prompts/get" => "name",
            "resources/read" => "uri",
            _ => null,
        };
        if (bodyNameParam != null)
        {
            var name = httpRequest.Headers[HeaderName];
            if (name == null)
            {
                return string.Format(Resources.Errors.McpHeaderMissing, HeaderName);
            }
            if (!TryDecodeHeaderValue(name, out var decodedName))
            {
                return string.Format(Resources.Errors.McpHeaderInvalid, HeaderName);
            }
            var bodyName = request.GetStringParam(bodyNameParam);
            if (decodedName != bodyName)
            {
                return string.Format(Resources.Errors.McpHeaderMismatch, HeaderName, name, bodyName);
            }
        }

        return null;
    }

    private static bool TryDecodeHeaderValue(string value, out string decodedValue)
    {
        decodedValue = value;
        if (!value.StartsWith(Base64Prefix, StringComparison.Ordinal)
            || !value.EndsWith(Base64Suffix, StringComparison.Ordinal)
            || value.Length < Base64Prefix.Length + Base64Suffix.Length)
        {
            return true;
        }

        var base64 = value[Base64Prefix.Length..^Base64Suffix.Length];
        try
        {
            decodedValue = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
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
        Close();
        return ValueTask.CompletedTask;
    }
}
