using System.Text.Json;

namespace QueryCat.Mcp.Models;

/// <summary>
/// The server's response to a <c>tools/call</c> request. The result contains a list of content
/// items and an optional flag indicating whether the tool call resulted in an error.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#calling-tools.
/// </remarks>
public sealed class McpCallToolResultModel : McpResultModel
{
    /// <summary>
    /// A list of content objects returned by the tool.
    /// </summary>
    public List<McpToolContentModel> Content { get; set; } = new();

    /// <summary>
    /// Whether the tool call ended in an error. Defaults to false.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// An optional object representing structured tool output. If provided, it must validate against
    /// the tool's <see cref="McpToolModel.OutputSchema" />.
    /// </summary>
    public JsonElement? StructuredContent { get; set; }

    /// <summary>
    /// Create error response.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>Instance of <see cref="McpCallToolResultModel" />.</returns>
    public static McpCallToolResultModel CreateError(string message)
    {
        return new McpCallToolResultModel
        {
            IsError = true,
            Content = new List<McpToolContentModel>
            {
                new McpToolTextContentModel
                {
                    Text = message
                }
            }
        };
    }

    /// <summary>
    /// Create successful response with structured content. The same content is also added
    /// as serialized text for clients that do not support structured content.
    /// </summary>
    /// <param name="structuredContent">Structured content.</param>
    /// <returns>Instance of <see cref="McpCallToolResultModel" />.</returns>
    public static McpCallToolResultModel CreateStructured(JsonElement structuredContent)
    {
        return new McpCallToolResultModel
        {
            Content =
            [
                new McpToolTextContentModel
                {
                    Text = structuredContent.ToString(),
                },
            ],
            StructuredContent = structuredContent,
        };
    }
}
