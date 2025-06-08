using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Storage for rows inputs and outputs configuration. Can be used to share data.
/// </summary>
public interface IConfigStorage
{
    /// <summary>
    /// Set value.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="value">Value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if storage has such key, <c>false</c> otherwise.
    /// </summary>
    /// <param name="key">Key to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>True</c> if key exists, <c>false</c> otherwise.</returns>
    ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get value by key.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Value.</returns>
    ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default);

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
