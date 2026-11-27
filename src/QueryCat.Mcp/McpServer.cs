using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// QueryCat MCP server. The server supports both the stateless protocol (2026-07-28), where every request
/// carries the protocol version and the client capabilities in its metadata, and the legacy
/// initialization-based protocol versions (2025-11-25 and 2025-06-18).
/// </summary>
public sealed class McpServer
{
    /// <summary>
    /// The latest version of the Model Context Protocol that this server supports.
    /// </summary>
    public const string ProtocolVersion = "2026-07-28";

    /// <summary>
    /// The latest legacy (initialization-based) version of the Model Context Protocol that this server supports.
    /// </summary>
    public const string LegacyProtocolVersion = "2025-11-25";

    private static readonly string[] _supportedProtocolVersions = [ProtocolVersion];

    // The 2025-03-26 version is not supported because it requires JSON-RPC batching.
    private static readonly string[] _supportedLegacyProtocolVersions = [LegacyProtocolVersion, "2025-06-18"];

    /// <summary>
    /// Max resource content size in bytes that can be returned to the client.
    /// </summary>
    public const long MaxResourceSize = 10 * 1024 * 1024;

    // Caching hints (milliseconds). The lists rarely change, and the change is also signaled with notifications.
    private const long DiscoverTtlMs = 60 * 60 * 1000;
    private const long ListTtlMs = 60 * 1000;
    private const long ReadResourceTtlMs = 0;

    private const string NotificationToolsListChanged = "notifications/tools/list_changed";
    private const string NotificationResourcesListChanged = "notifications/resources/list_changed";
    private const string NotificationSubscriptionsAcknowledged = "notifications/subscriptions/acknowledged";
    private const string NotificationCancelled = "notifications/cancelled";
    private const string NotificationInitialized = "notifications/initialized";

    private static readonly UTF8Encoding _strictUtf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IExecutionThread _executionThread;
    private readonly IMcpTransport _mcpTransport;
    private readonly Dictionary<string, IMcpTool> _tools = new();
    private readonly Dictionary<string, IMcpResource> _resources = new();
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();

    // Legacy protocol state: the "initialize" request has been received, the "notifications/initialized"
    // notification has been received (server notifications are allowed).
    private volatile bool _legacyInitializeReceived;
    private volatile bool _legacyInitialized;
    private readonly McpImplementationModel _serverInfo = new()
    {
        Name = Application.ProductName,
        Version = Application.GetShortVersion(),
        WebsiteUrl = @"https://github.com/krasninja/querycat",
    };

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpServer));

    /// <summary>
    /// Active "subscriptions/listen" request.
    /// </summary>
    /// <param name="Id">JSON-RPC id of the request, it is also the subscription id.</param>
    /// <param name="SpanId">Transport request identifier.</param>
    /// <param name="Filter">Notification types the server agreed to send.</param>
    private sealed record Subscription(object? Id, Guid SpanId, McpSubscriptionFilterModel Filter);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="executionThread">The execution thread.</param>
    /// <param name="mcpTransport">The transport to use.</param>
    public McpServer(IExecutionThread executionThread, IMcpTransport mcpTransport)
    {
        _executionThread = executionThread;
        _mcpTransport = mcpTransport;
    }

    /// <summary>
    /// Determines whether the protocol version is a supported legacy (initialization-based) one.
    /// </summary>
    /// <param name="protocolVersion">Protocol version.</param>
    /// <returns><c>True</c> if it is the legacy version, <c>false</c> otherwise.</returns>
    public static bool IsLegacyProtocolVersion(string protocolVersion)
        => Array.IndexOf(_supportedLegacyProtocolVersions, protocolVersion) > -1;

    /// <summary>
    /// Register a tool with the server. If the tool list changes, the clients subscribed
    /// with "subscriptions/listen" (and the legacy clients) are notified with "notifications/tools/list_changed".
    /// </summary>
    /// <param name="tool">The tool model.</param>
    public void AddTool(IMcpTool tool)
    {
        _tools[tool.Metadata.Name] = tool;
        _ = SendListChangedNotificationAsync(NotificationToolsListChanged, f => f.ToolsListChanged == true);
    }

    /// <summary>
    /// Remove tool.
    /// </summary>
    /// <param name="tool">The tool model.</param>
    public void RemoveTool(IMcpTool tool)
    {
        _tools.Remove(tool.Metadata.Name);
        _ = SendListChangedNotificationAsync(NotificationToolsListChanged, f => f.ToolsListChanged == true);
    }

    /// <summary>
    /// Register a resource with the server. The resource is identified by its URI.
    /// If the resource list changes, the clients subscribed with "subscriptions/listen" are
    /// notified with "notifications/resources/list_changed" (the legacy clients are notified as well).
    /// </summary>
    /// <param name="resource">The resource.</param>
    public void AddResource(IMcpResource resource)
    {
        _resources[resource.Metadata.Uri] = resource;
        _ = SendListChangedNotificationAsync(NotificationResourcesListChanged, f => f.ResourcesListChanged == true);
    }

    /// <summary>
    /// Remove resource.
    /// </summary>
    /// <param name="resource">The resource model.</param>
    public void RemoveResource(IMcpResource resource)
    {
        _resources.Remove(resource.Metadata.Uri);
        _ = SendListChangedNotificationAsync(NotificationResourcesListChanged, f => f.ResourcesListChanged == true);
    }

    private async Task SendListChangedNotificationAsync(string method, Func<McpSubscriptionFilterModel, bool> predicate)
    {
        // Legacy clients get the notifications without subscription.
        if (_legacyInitialized)
        {
            try
            {
                await _mcpTransport.SendNotificationAsync(new JsonRpcNotification { Method = method }, Guid.Empty);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Error sending '{Method}' notification.", method);
            }
        }

        foreach (var subscription in _subscriptions.Values)
        {
            if (!predicate.Invoke(subscription.Filter))
            {
                continue;
            }

            var notification = new JsonRpcNotification
            {
                Method = method,
                Params = new McpNotificationParamsModel
                {
                    Meta = new McpMetaModel
                    {
                        SubscriptionId = subscription.Id,
                    },
                },
            };
            try
            {
                if (!await _mcpTransport.SendNotificationAsync(notification, subscription.SpanId))
                {
                    // The client has disconnected.
                    _subscriptions.TryRemove(subscription.SpanId, out _);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Error sending '{Method}' notification.", method);
            }
        }
    }

    /// <summary>
    /// Process a single JSON-RPC message and return the response.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON-RPC response (success or error). For notifications and long-lived requests
    /// ("subscriptions/listen"), it is an empty <see cref="JsonRpcResponse" /> without result,
    /// the transport must not send it as a JSON-RPC message.</returns>
    public async Task<JsonRpcFrame> ProcessMessageAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        if (rpcRequest.IsNotification)
        {
            if (rpcRequest.Method == NotificationCancelled)
            {
                CancelSubscription(rpcRequest);
            }
            else if (rpcRequest.Method == NotificationInitialized)
            {
                _legacyInitialized = true;
            }
            _logger.LogTrace("Notification '{Method}'.", rpcRequest.Method);
            return new JsonRpcResponse();
        }

        if (IsLegacyRequest(rpcRequest))
        {
            return await ProcessLegacyMessageAsync(rpcRequest, cancellationToken);
        }

        var metaError = ValidateRequestMeta(rpcRequest);
        if (metaError != null)
        {
            return metaError;
        }

        var response = rpcRequest.Method switch
        {
            "server/discover" => ProcessDiscover(rpcRequest),
            "tools/list" => ProcessListTools(rpcRequest),
            "tools/call" => await ProcessCallToolAsync(rpcRequest, cancellationToken),
            "resources/list" => ProcessListResources(rpcRequest),
            "resources/templates/list" => new JsonRpcResponse(rpcRequest.Id, new McpListResourceTemplatesResultModel
            {
                TtlMs = ListTtlMs,
                CacheScope = McpCacheableResultModel.CacheScopePublic,
            }),
            "resources/read" => await ProcessReadResourceAsync(rpcRequest, legacy: false, cancellationToken),
            "subscriptions/listen" => await ProcessSubscriptionsListenAsync(rpcRequest, cancellationToken),
            _ => CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeMethodNotFound,
                string.Format(Resources.Errors.McpMethodNotFound, rpcRequest.Method)
            ),
        };

        // Identify the server in every result.
        if (response is JsonRpcResponse { Result: McpResultModel result })
        {
            result.Meta ??= new McpMetaModel();
            result.Meta.ServerInfo = _serverInfo;
        }
        return response;
    }

    /// <summary>
    /// Main server loop: read requests, process them, send responses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public async Task LoopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _mcpTransport.GetNextRequestAsync(cancellationToken);
            while (request != null)
            {
                JsonRpcFrame response;
                try
                {
                    response = await ProcessMessageAsync(request, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error processing '{Method}'.", request.Method);
                    response = request.IsNotification
                        ? new JsonRpcResponse()
                        : CreateError(request, McpErrorModel.ErrorCodeInternalError, Resources.Errors.McpInternalError);
                }
                response.SpanId = request.SpanId;
                await _mcpTransport.SendResponseAsync(response, cancellationToken);
                request = await _mcpTransport.GetNextRequestAsync(cancellationToken);
            }
        }
        finally
        {
            await CloseSubscriptionsAsync();
        }
    }

    /// <summary>
    /// Determines whether the request uses a legacy (initialization-based) protocol version.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request.</param>
    /// <returns><c>True</c> if it is a legacy request.</returns>
    private bool IsLegacyRequest(JsonRpcRequest rpcRequest)
    {
        // A request with the per-request protocol version is always modern.
        if (rpcRequest.GetProtocolVersion() != null)
        {
            return false;
        }
        // The legacy handshake.
        if (rpcRequest.Method == "initialize")
        {
            return true;
        }
        // Use the transport hint (HTTP header), otherwise the state (stdio process).
        return rpcRequest.IsLegacy ?? _legacyInitializeReceived;
    }

    private async Task<JsonRpcFrame> ProcessLegacyMessageAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        var response = rpcRequest.Method switch
        {
            "initialize" => ProcessLegacyInitialize(rpcRequest),
            "ping" => new JsonRpcResponse(rpcRequest.Id, new McpResultModel()),
            "tools/list" => ProcessListTools(rpcRequest),
            "tools/call" => await ProcessCallToolAsync(rpcRequest, cancellationToken),
            "resources/list" => ProcessListResources(rpcRequest),
            "resources/templates/list" => new JsonRpcResponse(rpcRequest.Id, new McpListResourceTemplatesResultModel()),
            "resources/read" => await ProcessReadResourceAsync(rpcRequest, legacy: true, cancellationToken),
            _ => CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeMethodNotFound,
                string.Format(Resources.Errors.McpMethodNotFound, rpcRequest.Method)
            ),
        };
        return response;
    }

    private JsonRpcFrame ProcessLegacyInitialize(JsonRpcRequest rpcRequest)
    {
        _legacyInitializeReceived = true;

        // If the server supports the requested protocol version, it must respond with the same version.
        // Otherwise, it must respond with another version it supports (the latest one).
        var protocolVersion = rpcRequest.GetStringParam("protocolVersion");
        if (protocolVersion == null || !IsLegacyProtocolVersion(protocolVersion))
        {
            protocolVersion = LegacyProtocolVersion;
        }

        var result = new McpInitializeResultModel
        {
            ProtocolVersion = protocolVersion,
            Capabilities = CreateCapabilities(),
            ServerInfo = _serverInfo,
            Instructions = Resources.Messages.McpInstructions,
        };
        _logger.LogTrace("Initialize request, protocol version {ProtocolVersion}.", protocolVersion);
        return new JsonRpcResponse(rpcRequest.Id, result);
    }

    private static McpServerCapabilitiesModel CreateCapabilities() => new()
    {
        Tools = new McpServerToolsCapabilityModel
        {
            ListChanged = true,
        },
        Resources = new McpServerResourcesCapabilityModel
        {
            Subscribe = false,
            ListChanged = true,
        },
    };

    /// <summary>
    /// Validate the protocol fields of the request metadata.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request.</param>
    /// <returns>Error response or <c>null</c> if the request is valid.</returns>
    private JsonRpcErrorResponse? ValidateRequestMeta(JsonRpcRequest rpcRequest)
    {
        var protocolVersion = rpcRequest.GetProtocolVersion();
        if (protocolVersion == null)
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                string.Format(Resources.Errors.McpRequestMetaRequired, JsonRpcRequest.MetaProtocolVersion));
        }
        if (Array.IndexOf(_supportedProtocolVersions, protocolVersion) < 0)
        {
            return CreateUnsupportedProtocolVersionError(rpcRequest, protocolVersion);
        }
        if (rpcRequest.GetMeta(JsonRpcRequest.MetaClientCapabilities) is not { ValueKind: JsonValueKind.Object })
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                string.Format(Resources.Errors.McpRequestMetaRequired, JsonRpcRequest.MetaClientCapabilities));
        }
        return null;
    }

    private JsonRpcErrorResponse CreateUnsupportedProtocolVersionError(JsonRpcRequest rpcRequest, string requestedVersion)
    {
        _logger.LogDebug("Unsupported protocol version '{ProtocolVersion}'.", requestedVersion);
        return new JsonRpcErrorResponse(new McpErrorModel(
            McpErrorModel.ErrorCodeUnsupportedProtocolVersion,
            string.Format(Resources.Errors.McpUnsupportedProtocolVersion, requestedVersion,
                string.Join(", ", _supportedProtocolVersions)),
            new McpUnsupportedProtocolVersionDataModel
            {
                Supported = _supportedProtocolVersions.ToList(),
                Requested = requestedVersion,
            }))
        {
            Id = rpcRequest.Id,
        };
    }

    private JsonRpcFrame ProcessDiscover(JsonRpcRequest rpcRequest)
    {
        var result = new McpDiscoverResultModel
        {
            SupportedVersions = _supportedProtocolVersions.ToList(),
            Capabilities = CreateCapabilities(),
            Instructions = Resources.Messages.McpInstructions,
            TtlMs = DiscoverTtlMs,
            CacheScope = McpCacheableResultModel.CacheScopePublic,
        };
        _logger.LogTrace("Discover request.");
        return new JsonRpcResponse(rpcRequest.Id, result);
    }

    private JsonRpcFrame ProcessListTools(JsonRpcRequest rpcRequest)
    {
        var result = new McpListToolsResultModel
        {
            // Deterministic order allows the client-side caching.
            Tools = _tools.Values
                .Select(t => t.Metadata)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList(),
            TtlMs = ListTtlMs,
            CacheScope = McpCacheableResultModel.CacheScopePublic,
        };
        _logger.LogTrace("List tools request, {Count} tool(s).", _tools.Count);
        return new JsonRpcResponse(rpcRequest.Id, result);
    }

    private async Task<JsonRpcFrame> ProcessSubscriptionsListenAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        if (rpcRequest.Params == null
            || !rpcRequest.Params.TryGetValue("notifications", out var filterElement)
            || filterElement.ValueKind != JsonValueKind.Object)
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                Resources.Errors.McpSubscriptionFilterRequired
            );
        }

        // Acknowledge only the supported notification types.
        var requestedFilter = filterElement.Deserialize(SourceGenerationContext.Default.McpSubscriptionFilterModel)
            ?? new McpSubscriptionFilterModel();
        var filter = new McpSubscriptionFilterModel
        {
            ToolsListChanged = requestedFilter.ToolsListChanged == true ? true : null,
            ResourcesListChanged = requestedFilter.ResourcesListChanged == true ? true : null,
        };

        // The acknowledgement must be the first message of the subscription, register it after.
        var acknowledgement = new JsonRpcNotification
        {
            Method = NotificationSubscriptionsAcknowledged,
            Params = new McpSubscriptionsAcknowledgedParamsModel
            {
                Meta = new McpMetaModel
                {
                    SubscriptionId = rpcRequest.Id,
                },
                Notifications = filter,
            },
        };
        if (await _mcpTransport.SendNotificationAsync(acknowledgement, rpcRequest.SpanId, cancellationToken))
        {
            _subscriptions[rpcRequest.SpanId] = new Subscription(rpcRequest.Id, rpcRequest.SpanId, filter);
            _logger.LogTrace("Subscription {SubscriptionId} is open.", rpcRequest.Id);
        }

        // The stream stays open until the client or server closes it.
        return new JsonRpcResponse();
    }

    private void CancelSubscription(JsonRpcRequest rpcRequest)
    {
        if (rpcRequest.Params == null
            || !rpcRequest.Params.TryGetValue("requestId", out var requestIdElement))
        {
            return;
        }

        var requestId = GetIdKey(requestIdElement);
        foreach (var subscription in _subscriptions.Values)
        {
            if (GetIdKey(subscription.Id) == requestId)
            {
                _subscriptions.TryRemove(subscription.SpanId, out _);
                _logger.LogTrace("Subscription {SubscriptionId} is cancelled.", requestId);
            }
        }
    }

    private static string? GetIdKey(object? id) => id switch
    {
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.GetRawText(),
        _ => id?.ToString(),
    };

    private async Task CloseSubscriptionsAsync()
    {
        // Signal the graceful end of the subscriptions.
        foreach (var subscription in _subscriptions.Values)
        {
            _subscriptions.TryRemove(subscription.SpanId, out _);
            var response = new JsonRpcResponse(subscription.Id, new McpResultModel
            {
                Meta = new McpMetaModel
                {
                    ServerInfo = _serverInfo,
                    SubscriptionId = subscription.Id,
                },
            })
            {
                SpanId = subscription.SpanId,
            };
            try
            {
                await _mcpTransport.SendResponseAsync(response);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Cannot close subscription {SubscriptionId}.", subscription.Id);
            }
        }
    }

    private async Task<JsonRpcFrame> ProcessCallToolAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        if (rpcRequest.Params == null
            || !rpcRequest.Params.TryGetValue("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                Resources.Errors.McpToolNameRequired
            );
        }

        var toolName = nameElement.GetString()!;
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                string.Format(Resources.Errors.McpUnknownTool, toolName)
            );
        }

        // Parse optional arguments.
        Dictionary<string, JsonElement>? arguments = null;
        if (rpcRequest.Params.TryGetValue("arguments", out var argumentsElement)
            && argumentsElement.ValueKind == JsonValueKind.Object)
        {
            arguments = argumentsElement.Deserialize(SourceGenerationContext.Default.DictionaryStringJsonElement);
        }
        arguments ??= new Dictionary<string, JsonElement>();

        _logger.LogTrace("Call tool '{ToolName}'.", toolName);
        McpCallToolResultModel result;
        try
        {
            result = await tool.CallAsync(_executionThread, arguments, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Report tool execution errors as the tool result, so the client can see it and retry.
            _logger.LogDebug(e, "Error calling tool '{ToolName}'.", toolName);
            result = McpCallToolResultModel.CreateError(e.Message);
        }
        return new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
    }

    private JsonRpcFrame ProcessListResources(JsonRpcRequest rpcRequest)
    {
        var result = new McpListResourcesResultModel
        {
            Resources = _resources.Values
                .Select(r => r.Metadata)
                .OrderBy(r => r.Uri, StringComparer.Ordinal)
                .ToList(),
            TtlMs = ListTtlMs,
            CacheScope = McpCacheableResultModel.CacheScopePublic,
        };
        _logger.LogTrace("List resources request, {Count} resource(s).", _resources.Count);
        return new JsonRpcResponse(rpcRequest.Id, result);
    }

    private async Task<JsonRpcFrame> ProcessReadResourceAsync(JsonRpcRequest rpcRequest, bool legacy, CancellationToken cancellationToken)
    {
        if (rpcRequest.Params == null
            || !rpcRequest.Params.TryGetValue("uri", out var uriElement)
            || uriElement.ValueKind != JsonValueKind.String)
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInvalidParams,
                Resources.Errors.McpResourceUriRequired
            );
        }

        var uri = uriElement.GetString()!;
        if (!_resources.TryGetValue(uri, out var resource))
        {
            return CreateError(
                rpcRequest,
                // The error code has been changed in 2026-07-28.
                legacy ? McpErrorModel.ErrorCodeLegacyResourceNotFound : McpErrorModel.ErrorCodeInvalidParams,
                string.Format(Resources.Errors.McpResourceNotFound, uri)
            );
        }

        _logger.LogTrace("Read resource '{Uri}'.", uri);
        byte[] data;
        try
        {
            data = await ReadResourceDataAsync(resource, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogDebug(e, "Error reading resource '{Uri}'.", uri);
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeInternalError,
                string.Format(Resources.Errors.McpResourceReadError, uri, e.Message)
            );
        }

        return new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = new McpReadResourceResultModel
            {
                Contents = [CreateResourceContents(uri, resource.Metadata.MimeType, data)],
                // The content may change at any time and may contain private data.
                TtlMs = ReadResourceTtlMs,
                CacheScope = McpCacheableResultModel.CacheScopePrivate,
            },
        };
    }

    private static async Task<byte[]> ReadResourceDataAsync(IMcpResource resource, CancellationToken cancellationToken)
    {
        await using var stream = await resource.OpenAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memoryStream.Length + bytesRead > MaxResourceSize)
            {
                throw new QueryCatException(string.Format(Resources.Errors.McpResourceTooLarge, MaxResourceSize));
            }
            memoryStream.Write(buffer, 0, bytesRead);
        }
        return memoryStream.ToArray();
    }

    private static McpResourceContentsModel CreateResourceContents(string uri, string? mimeType, byte[] data)
    {
        var contents = new McpResourceContentsModel
        {
            Uri = uri,
            MimeType = mimeType,
        };
        if (mimeType == null || IsTextMimeType(mimeType))
        {
            // Unknown MIME type: return as text if the content looks like a UTF-8 text.
            if (TryDecodeText(data, out var text))
            {
                contents.Text = text;
                contents.MimeType ??= "text/plain";
                return contents;
            }
        }
        contents.Blob = Convert.ToBase64String(data);
        contents.MimeType ??= System.Net.Mime.MediaTypeNames.Application.Octet;
        return contents;
    }

    private static bool IsTextMimeType(string mimeType)
        => mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
           || mimeType.EndsWith("json", StringComparison.OrdinalIgnoreCase)
           || mimeType.EndsWith("xml", StringComparison.OrdinalIgnoreCase)
           || mimeType.EndsWith("yaml", StringComparison.OrdinalIgnoreCase)
           || mimeType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase);

    private static bool TryDecodeText(byte[] data, out string text)
    {
        try
        {
            text = _strictUtf8Encoding.GetString(data);
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
        if (text.Contains('\0'))
        {
            text = string.Empty;
            return false;
        }
        // Remove BOM.
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }
        return true;
    }

    private static JsonRpcErrorResponse CreateError(JsonRpcRequest rpcRequest, int code, string message)
        => new(new McpErrorModel(code, message))
        {
            Id = rpcRequest.Id,
        };
}
