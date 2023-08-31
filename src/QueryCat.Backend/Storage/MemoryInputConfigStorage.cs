using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// In memory storage for rows input.
/// </summary>
public class MemoryInputConfigStorage : IInputConfigStorage
{
    protected Dictionary<string, VariantValue> Map { get; } = new();

    /// <inheritdoc />
    public virtual void Set(string key, VariantValue value)
    {
        if (value.IsNull)
        {
            Map.Remove(key);
        }
        else
        {
            Map[key] = value;
        }
    }

    /// <inheritdoc />
    public virtual bool Has(string key) => Map.ContainsKey(key);

    /// <inheritdoc />
    public virtual VariantValue Get(string key)
    {
        if (Map.TryGetValue(key, out var value))
        {
            return value;
        }
        return VariantValue.Null;
    }

    /// <inheritdoc />
    public virtual Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
