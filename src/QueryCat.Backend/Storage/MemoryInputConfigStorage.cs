using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// In memory storage for rows input.
/// </summary>
public class MemoryInputConfigStorage : IInputConfigStorage
{
    protected Dictionary<string, VariantValue> Map { get; } = new();

    /// <inheritdoc />
    public void Set(string key, VariantValue value)
    {
        Map[key] = value;
    }

    /// <inheritdoc />
    public void Unset(string key)
    {
        Map.Remove(key);
    }

    /// <inheritdoc />
    public bool Has(string key) => Map.ContainsKey(key);

    /// <inheritdoc />
    public VariantValue Get(string key) => Map[key];
}
