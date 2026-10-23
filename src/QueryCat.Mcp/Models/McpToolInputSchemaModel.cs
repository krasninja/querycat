using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// A JSON Schema object defining the expected parameters for a tool.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#tool.
/// </remarks>
public sealed class McpToolInputSchemaModel
{
    /// <summary>
    /// The type of the schema. Must be "object".
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// The properties of the schema. Each key is a parameter name, and each value is a JSON Schema
    /// object defining the expected structure of that parameter.
    /// </summary>
    public Dictionary<string, McpToolSchemaPropertyModel> Properties { get; set; } = new();

    /// <summary>
    /// A list of the names of required parameters.
    /// </summary>
    public string[] Required { get; set; } = [];

    /// <summary>
    /// Additional properties.
    /// </summary>
    public bool AdditionalProperties { get; set; }
}
