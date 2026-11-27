namespace QueryCat.Mcp.Models;

/// <summary>
/// A response to a request that indicates an error occurred.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#jsonrpcerrorresponse.
/// </remarks>
public class JsonRpcErrorResponse : JsonRpcFrame
{
    /// <summary>
    /// Error.
    /// </summary>
    public McpErrorModel Error { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="errorModel">Error.</param>
    public JsonRpcErrorResponse(McpErrorModel errorModel)
    {
        Error = errorModel;
    }
}
