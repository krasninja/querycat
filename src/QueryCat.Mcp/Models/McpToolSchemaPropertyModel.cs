namespace QueryCat.Mcp.Models;

/// <summary>
/// A JSON schema input or output parameter.
/// </summary>
public sealed class McpToolSchemaPropertyModel
{
    public const string TypeString = "string";
    public const string TypeInteger = "integer";
    public const string TypeBoolean = "boolean";
    public const string TypeArray = "array";
    public const string TypeObject = "object";

    public string Type { get; private set; }

    public string Description { get; private set; }

    public string[]? Enum { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public IDictionary<string, McpToolSchemaPropertyModel>? Items { get; set; }

    public McpToolSchemaPropertyModel(string type, string? description = null)
    {
        Type = type;
        Description = description ?? string.Empty;
    }
}
