using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Extensions for <see cref="IConfigStorage" />.
/// </summary>
public static class ConfigStorageExtensions
{
    /// <summary>
    /// Get value from the storage or set if it doesn't exist.
    /// </summary>
    /// <param name="configStorage">Instance of <see cref="IConfigStorage" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="func">Delegate to create value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Existing or new value.</returns>
    public static async ValueTask<VariantValue> GetOrSetAsync(
        this IConfigStorage configStorage,
        string key,
        Func<string, VariantValue> func,
        CancellationToken cancellationToken = default)
    {
        if (!await configStorage.HasAsync(key, cancellationToken))
        {
            var value = func(key);
            await configStorage.SetAsync(key, value, cancellationToken);
            return value;
        }
        return await configStorage.GetAsync(key, cancellationToken);
    }

    /// <summary>
    /// Get value from the storage or default.
    /// </summary>
    /// <param name="configStorage">Instance of <see cref="IConfigStorage" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="default">Default value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    public static async ValueTask<VariantValue> GetOrDefaultAsync(
        this IConfigStorage configStorage,
        string key,
        VariantValue @default = default,
        CancellationToken cancellationToken = default)
    {
        if (!await configStorage.HasAsync(key, cancellationToken))
        {
            return @default;
        }
        return await configStorage.GetAsync(key, cancellationToken);
    }
}
