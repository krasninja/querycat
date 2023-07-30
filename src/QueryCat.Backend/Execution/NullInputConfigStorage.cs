using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Empty input config storage implementation.
/// </summary>
public class NullInputConfigStorage : IInputConfigStorage
{
    public static NullInputConfigStorage Instance { get; } = new();

    /// <inheritdoc />
    public void Set(string key, VariantValue value)
    {
    }

    /// <inheritdoc />
    public void Unset(string key)
    {
    }

    /// <inheritdoc />
    public bool Has(string key) => false;

    /// <inheritdoc />
    public VariantValue Get(string key) => VariantValue.Null;

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
