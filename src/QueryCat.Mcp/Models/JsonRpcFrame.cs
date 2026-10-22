using System.Text.Json.Serialization;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Base class for JSON RPC with version and id.
/// </summary>
public class JsonRpcFrame
{
    /// <summary>
    /// JSON RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// A uniquely identifying ID for a request in JSON-RPC.
    /// </summary>
    public object? Id { get; set; }

    /// <summary>
    /// Unique request identifier.
    /// </summary>
    [JsonIgnore]
    public Guid SpanId { get; set; }
}
