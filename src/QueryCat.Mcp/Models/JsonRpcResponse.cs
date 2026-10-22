namespace QueryCat.Mcp.Models;

/// <summary>
/// A successful (non-error) response to a request.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#jsonrpcresultresponse.
/// </remarks>
public class JsonRpcResponse : JsonRpcFrame
{
    /// <summary>
    /// Result object.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Constructor. The response without result is an acknowledgement (for example, of a notification),
    /// the transport must not send it as a JSON-RPC message.
    /// </summary>
    public JsonRpcResponse()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="result">Result object.</param>
    public JsonRpcResponse(object? result)
    {
        Result = result;
    }
}
