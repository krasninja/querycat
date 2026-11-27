namespace QueryCat.Mcp.Models;

/// <summary>
/// The result with caching hints for the client.
/// </summary>
/// <remarks>
/// https://modelcontextprotocol.io/specification/2026-07-28/server/utilities/caching.
/// </remarks>
public class McpCacheableResultModel : McpResultModel
{
    /// <summary>
    /// The response does not contain user-specific data and can be shared.
    /// </summary>
    public const string CacheScopePublic = "public";

    /// <summary>
    /// The response contains private data that is not meant to be shared between callers.
    /// </summary>
    public const string CacheScopePrivate = "private";

    /// <summary>
    /// How long, in milliseconds, the client may consider the result fresh. Zero means immediately stale.
    /// </summary>
    public long TtlMs { get; set; }

    /// <summary>
    /// The intended scope of the cached response: "public" or "private".
    /// </summary>
    public string CacheScope { get; set; } = CacheScopePublic;
}
