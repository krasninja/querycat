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

    private static readonly string[] _supportedProtocolVersions = [ProtocolVersion, "2025-06-18", "2025-03-26"];

    private readonly IExecutionThread _executionThread;
    private readonly IMcpTransport _mcpTransport;
    private readonly Dictionary<string, IMcpTool> _tools = new();

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
                _logger.LogDebug(e, "Error processing '{Method}'.", request.Method);
                response = request.IsNotification
                    ? new JsonRpcResponse()
                    : CreateError(request, McpErrorModel.ErrorCodeInternalError, e.Message);
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
                Resources.Errors.McpInvalidRequest
            );
        }

        var toolName = nameElement.GetString()!;
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            return CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeMethodNotFound,
                string.Format(Resources.Errors.McpMethodNotFound, toolName)
            );
        }

        // Parse optional arguments.
        Dictionary<string, JsonElement>? arguments = null;
        if (rpcRequest.Params.TryGetValue("arguments", out var argumentsElement)
            && argumentsElement.ValueKind == JsonValueKind.Object)
        {
            arguments = argumentsElement.Deserialize(SourceGenerationContext.Default.DictionaryStringJsonElement);
        }
        if (arguments == null)
        {
            arguments = new Dictionary<string, JsonElement>();
        }

        _logger.LogTrace("Call tool '{ToolName}'.", toolName);
        var result = await tool.CallAsync(_executionThread, arguments, cancellationToken);
        return new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
    }

    private static JsonRpcErrorResponse CreateError(JsonRpcRequest rpcRequest, int code, string message)
        => new(new McpErrorModel(code, message))
        {
            Id = rpcRequest.Id,
        };
}
