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

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpServer));

    public McpServer(IExecutionThread executionThread, IMcpTransport mcpTransport)
    {
        _executionThread = executionThread;
        _mcpTransport = mcpTransport;
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
            "ping" => new JsonRpcResponse(new McpEmptyResultModel()) { Id = rpcRequest.Id },
            _ => CreateError(
                rpcRequest,
                McpErrorModel.ErrorCodeMethodNotFound,
                string.Format(Resources.Messages.McpMethodNotFound, rpcRequest.Method)
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
            Capabilities = new McpServerCapabilitiesModel(),
            ServerInfo = new McpImplementationModel
            {
                Name = Application.ProductName,
                Version = Application.GetShortVersion(),
            },
        };
        var response = new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result,
        };
        _logger.LogTrace("Initialize request, protocol version {ProtocolVersion}.", protocolVersion);
        return Task.FromResult<JsonRpcFrame>(response);
    }

    private static JsonRpcErrorResponse CreateError(JsonRpcRequest rpcRequest, int code, string message)
        => new(new McpErrorModel(code, message))
        {
            Id = rpcRequest.Id,
        };
}
