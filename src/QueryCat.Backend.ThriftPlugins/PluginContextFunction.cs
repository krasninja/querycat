using System.Text.Json.Serialization;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed record PluginContextFunction(
    [property:JsonPropertyName("sig")] string Signature,
    [property:JsonPropertyName("desc")] string Description);
