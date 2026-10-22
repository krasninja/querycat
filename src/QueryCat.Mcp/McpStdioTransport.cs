using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Mcp.Models;

namespace QueryCat.Mcp;

/// <summary>
/// MCP transport over standard I/O (stdin/stdout) using newline-delimited JSON.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/basic/transports#stdio.
/// </remarks>
public sealed class McpStdioTransport : IMcpTransport, IAsyncDisposable
{
    private readonly Lock _writeLock = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(McpStdioTransport));
    private bool _started;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return Task.CompletedTask;
        }

        // Ensure UTF-8 without BOM for protocol I/O.
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        _started = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _started = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<JsonRpcRequest?> GetNextRequestAsync(CancellationToken cancellationToken = default)
    {
        EnsureStarted();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                // EOF: stdin closed by the client.
                return null;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var request = JsonSerializer.Deserialize(line, SourceGenerationContext.Default.JsonRpcRequest);
                if (request != null)
                {
                    return request;
                }
            }
            catch (JsonException ex)
            {
                // Stdout is reserved for protocol messages, so report the error to the client.
                _logger.LogDebug(ex, "Failed to deserialize JSON-RPC request from stdin.");
                await SendResponseAsync(
                    new JsonRpcErrorResponse(new McpErrorModel(McpErrorModel.ErrorCodeParseError, ex.Message)),
                    cancellationToken);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task SendResponseAsync(JsonRpcFrame response, CancellationToken cancellationToken = default)
    {
        EnsureStarted();

        lock (_writeLock)
        {
            if (response is JsonRpcResponse { Result: null })
            {
                // Acknowledgement of a notification, nothing to send.
                return Task.CompletedTask;
            }
            if (response is JsonRpcResponse successResponse)
            {
                var json = JsonSerializer.Serialize(successResponse, SourceGenerationContext.Default.JsonRpcResponse);
                Console.Out.WriteLine(json);
            }
            else if (response is JsonRpcErrorResponse errorResponse)
            {
                var json = JsonSerializer.Serialize(errorResponse, SourceGenerationContext.Default.JsonRpcErrorResponse);
                Console.Out.WriteLine(json);
            }

            Console.Out.Flush();
        }

        return Task.CompletedTask;
    }

    private void EnsureStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException("The transport has not been started.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _started = false;
        return ValueTask.CompletedTask;
    }
}
