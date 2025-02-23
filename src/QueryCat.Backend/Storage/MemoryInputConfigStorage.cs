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
    public virtual ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default)
    {
        if (value.IsNull)
        {
            Map.Remove(key);
        }
        else
        {
            Map[key] = value;
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public virtual ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Map.ContainsKey(key));
    }

    /// <inheritdoc />
    public virtual ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (Map.TryGetValue(key, out var value))
        {
            return ValueTask.FromResult(value);
        }
        return ValueTask.FromResult(VariantValue.Null);
    }

    /// <inheritdoc />
    public virtual Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
