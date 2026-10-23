using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// Tools enable models to interact with external systems, such as querying databases,
/// calling APIs, or performing computations. Each tool is uniquely identified by
/// a name and includes metadata describing its schema.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#tool.
/// </remarks>
public class McpToolModel
{
    /// <summary>
    /// A unique identifier for the tool. The name must be an ASCII string consisting
    /// of letters, digits, periods, hyphens, and underscores.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// A human-readable name for the tool.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// A human-readable description of the tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A JSON Schema object defining the expected parameters for the tool.
    /// </summary>
    public McpToolInputSchemaModel InputSchema { get; set; } = new();

    /// <summary>
    /// An optional JSON Schema object defining the structure of the tool's output
    /// returned in the <see cref="McpCallToolResultModel.StructuredContent" /> field of a
    /// <see cref="McpCallToolResultModel" />.
    /// </summary>
    public JsonElement? OutputSchema { get; set; }

    /// <summary>
    /// Optional annotations for the tool.
    /// </summary>
    public McpToolAnnotationsModel? Annotations { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="title">Title.</param>
    public McpToolModel(string name, string title)
    {
        Name = name;
        Title = title;
    }
}
