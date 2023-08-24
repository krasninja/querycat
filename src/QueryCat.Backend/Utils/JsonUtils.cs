using System.Text.Json.Nodes;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Utils;

/// <summary>
/// JSON utilities.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Get instance of <see cref="VariantValue" /> by simple
    /// path like "field1.nextField". If path is incorrect the return value is null.
    /// </summary>
    /// <param name="node">Instance of <see cref="JsonNode" />.</param>
    /// <param name="path">Fields path.</param>
    /// <returns>Value or null.</returns>
    public static VariantValue GetValueByPath(this JsonNode node, string path)
    {
        var currentNode = node;
        foreach (var part in path.Split('.'))
        {
            if (int.TryParse(part, out var indexPart))
            {
                currentNode = currentNode[indexPart];
            }
            else
            {
                currentNode = currentNode[part];
            }

            if (currentNode == null)
            {
                return VariantValue.Null;
            }
        }
        return VariantValue.CreateFromObject(currentNode);
    }
}
