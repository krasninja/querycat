namespace QueryCat.Mcp.Models;

/// <summary>
/// Audio content returned from a tool call.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2025-11-25/server/tools#audiocontent.
/// </remarks>
public sealed class McpToolAudioContentModel : McpToolContentModel
{
    /// <summary>
    /// The base64-encoded audio data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the audio. Different providers may support different audio types.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
}
