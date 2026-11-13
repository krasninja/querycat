using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// QueryCat MCP server.
/// </summary>
public sealed class McpServer
{
    /// <summary>
    /// The latest version of the Model Context Protocol that this server supports.
    /// </summary>
    public const string ProtocolVersion = "2025-11-25";

    // The 2025-03-26 version is not supported because it requires JSON-RPC batching.
    private static readonly string[] _supportedProtocolVersions = [ProtocolVersion, "2025-06-18"];

    /// <summary>
    /// Max resource content size in bytes that can be returned to the client.
    /// </summary>
    public const long MaxResourceSize = 10 * 1024 * 1024;

    private static readonly UTF8Encoding _strictUtf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IExecutionThread _executionThread;
    private readonly IMcpTransport _mcpTransport;
    private readonly Dictionary<string, IMcpTool> _tools = new();
    private readonly Dictionary<string, IMcpResource> _resources = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpServer));

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
    /// Register a tool with the server.
    /// </summary>
    /// <param name="tool">The tool model.</param>
    public void AddTool(IMcpTool tool)
    {
        _tools[tool.Metadata.Name] = tool;
    }

    /// <summary>
    /// Register a resource with the server. The resource is identified by its URI.
    /// </summary>
    /// <param name="resource">The resource.</param>
    public void AddResource(IMcpResource resource)
    {
        _resources[resource.Metadata.Uri] = resource;
    }

    /// <summary>
    /// Process a single JSON-RPC message and return the response.
    /// </summary>
    /// <param name="rpcRequest">The JSON-RPC request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON-RPC response (success or error). For notifications, it is an empty <see cref="JsonRpcResponse" />
    /// without result, the transport must not send it as a JSON-RPC message.</returns>
    public async Task<JsonRpcFrame> ProcessMessageAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken = default)
    {
        if (rpcRequest.IsNotification)
        {
            _logger.LogTrace("Notification '{Method}'.", rpcRequest.Method);
            return new JsonRpcResponse();
        }

        return rpcRequest.Method switch
        {
            "initialize" => await ProcessInitializeAsync(rpcRequest, cancellationToken),
            "ping" => new JsonRpcResponse(rpcRequest.Id, new McpEmptyResultModel()),
            "tools/list" => await ProcessListToolsAsync(rpcRequest, cancellationToken),
            "tools/call" => await ProcessCallToolAsync(rpcRequest, cancellationToken),
            "resources/list" => await ProcessListResourcesAsync(rpcRequest, cancellationToken),
            "resources/templates/list" => new JsonRpcResponse(rpcRequest.Id, new McpListResourceTemplatesResultModel()),
            "resources/read" => await ProcessReadResourceAsync(rpcRequest, cancellationToken),
            _ => CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeMethodNotFound,
                string.Format(Resources.Errors.McpMethodNotFound, rpcRequest.Method)
            ),
        };
    }

    /// <summary>
    /// Main server loop: read requests, process them, send responses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public async Task LoopAsync(CancellationToken cancellationToken = default)
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

    private Task<JsonRpcFrame> ProcessInitializeAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        // If the server supports the requested protocol version, it must respond with the same version.
        // Otherwise, it must respond with another version it supports (the latest one).
        var protocolVersion = ProtocolVersion;
        if (rpcRequest.Params != null
            && rpcRequest.Params.TryGetValue("protocolVersion", out var requestedVersion)
            && requestedVersion.ValueKind == JsonValueKind.String
            && Array.IndexOf(_supportedProtocolVersions, requestedVersion.GetString()) > -1)
        {
            protocolVersion = requestedVersion.GetString()!;
        }

        var result = new McpInitializeResultModel
        {
            ProtocolVersion = protocolVersion,
            Capabilities = new McpServerCapabilitiesModel
            {
                Tools = new McpServerToolsCapabilityModel
                {
                    ListChanged = false,
                },
                Resources = _resources.Count > 0
                    ? new McpServerResourcesCapabilityModel
                    {
                        Subscribe = false,
                        ListChanged = false,
                    }
                    : null,
            },
            ServerInfo = new McpImplementationModel
            {
                Name = Application.ProductName,
                Version = Application.GetShortVersion(),
                WebsiteUrl = @"https://github.com/krasninja/querycat",
            },
            Instructions = Resources.Messages.McpInstructions,
        };
        var response = new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
        _logger.LogTrace("Initialize request, protocol version {ProtocolVersion}.", protocolVersion);
        return Task.FromResult<JsonRpcFrame>(response);
    }

    private Task<JsonRpcFrame> ProcessListToolsAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        var result = new McpListToolsResultModel
        {
            Tools = _tools.Values.Select(t => t.Metadata).ToList(),
        };
        var response = new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
        _logger.LogTrace("List tools request, {Count} tool(s).", _tools.Count);
        return Task.FromResult<JsonRpcFrame>(response);
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

    private Task<JsonRpcFrame> ProcessListResourcesAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
    {
        var result = new McpListResourcesResultModel
        {
            Resources = _resources.Values.Select(r => r.Metadata).ToList(),
        };
        var response = new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
        _logger.LogTrace("List resources request, {Count} resource(s).", _resources.Count);
        return Task.FromResult<JsonRpcFrame>(response);
    }

    private async Task<JsonRpcFrame> ProcessReadResourceAsync(JsonRpcRequest rpcRequest, CancellationToken cancellationToken)
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
                McpErrorModel.ErrorCodeResourceNotFound,
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
