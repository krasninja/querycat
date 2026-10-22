namespace QueryCat.Mcp.Models;

/// <summary>
/// MCP error.
/// </summary>
public sealed class McpErrorModel
{
    // https://www.jsonrpc.org/specification

    /// <summary>
    /// Invalid JSON was received by the server.
    /// </summary>
    public const int ErrorCodeParseError = -32700;

    /// <summary>
    /// The JSON sent is not a valid request object.
    /// </summary>
    public const int ErrorCodeInvalidRequest = -32600;

    /// <summary>
    /// The method does not exist or is not available.
    /// </summary>
    public const int ErrorCodeMethodNotFound = -32601;

    /// <summary>
    /// Invalid method parameters.
    /// </summary>
    public const int ErrorCodeInvalidParams = -32602;

    /// <summary>
    /// Internal JSON-RPC error.
    /// </summary>
    public const int ErrorCodeInternalError = -32603;

    // https://modelcontextprotocol.io/specification/2026-07-28/basic#error-codes

    /// <summary>
    /// HTTP headers do not match the request body.
    /// </summary>
    public const int ErrorCodeHeaderMismatch = -32020;

    /// <summary>
    /// The client does not declare a capability required by the request.
    /// </summary>
    public const int ErrorCodeMissingRequiredClientCapability = -32021;

    /// <summary>
    /// The protocol version is not supported.
    /// </summary>
    public const int ErrorCodeUnsupportedProtocolVersion = -32022;

    /// <summary>
    /// The error type that occurred.
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// A short description of the error. The message SHOULD be limited to a concise single sentence.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Additional information about the error. The value of this member is defined by the
    /// sender (e.g. detailed error information, nested errors etc.).
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="code">The error type that occurred.</param>
    /// <param name="message">A short description of the error.</param>
    /// <param name="data">Additional information about the error.</param>
    public McpErrorModel(int code, string message, object? data = null)
    {
        Code = code;
        Message = message;
        Data = data;
    }
}
