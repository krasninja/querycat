using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Storage for rows input config.
/// </summary>
public interface IInputConfigStorage
{
    /// <summary>
    /// Set value.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">Value.</param>
    void Set(string key, VariantValue value);

    /// <summary>
    /// Remove value by key.
    /// </summary>
    /// <param name="key">Key.</param>
    void Unset(string key);

    /// <summary>
    /// Returns <c>true</c> if storage has such key, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">Key to check.</param>
    /// <returns><c>True</c> if key exists, <c>false</c> otherwise.</returns>
    bool Has(string key);

    /// <summary>
    /// Get value by key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>Value.</returns>
    VariantValue Get(string key);

    /// <summary>
    /// Save state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Load state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task LoadAsync(CancellationToken cancellationToken = default);
}
