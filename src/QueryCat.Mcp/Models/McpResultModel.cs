namespace QueryCat.Mcp.Models;

/// <summary>
/// Common MCP result model.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/schema#result.
/// </remarks>
public class McpResultModel
{
    /// <summary>
    /// The result is complete.
    /// </summary>
    public const string ResultComplete = "complete";

    /// <summary>
    /// The request requires additional input from the client.
    /// </summary>
    public const string ResultInputRequired = "input_required";

    /// <summary>
    /// Indicates the type of the result, which allows the client to determine how to parse the result object.
    /// </summary>
    public string ResultType { get; set; } = ResultComplete;
}
