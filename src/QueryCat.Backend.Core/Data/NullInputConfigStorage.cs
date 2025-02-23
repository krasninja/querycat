using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Empty input config storage implementation.
/// </summary>
public class NullInputConfigStorage : IInputConfigStorage
{
    public static NullInputConfigStorage Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default) => ValueTask.FromResult(false);

    /// <inheritdoc />
    public ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default) => ValueTask.FromResult(VariantValue.Null);

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
