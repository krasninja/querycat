using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IInputConfigStorage" />.
/// </summary>
public static class InputConfigStorageExtensions
{
    /// <summary>
    /// Get value from the storage or set if it doesn't exist.
    /// </summary>
    /// <param name="configStorage">Instance of <see cref="IInputConfigStorage" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="func">Delegate to create value.</param>
    /// <returns>Existing or new value.</returns>
    public static VariantValue GetOrSet(this IInputConfigStorage configStorage, string key, Func<string, VariantValue> func)
    {
        if (!configStorage.Has(key))
        {
            var value = func(key);
            configStorage.Set(key, value);
            return value;
        }
        return configStorage.Get(key);
    }

    /// <summary>
    /// Get value from the storage or default.
    /// </summary>
    /// <param name="configStorage">Instance of <see cref="IInputConfigStorage" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="default">Default value.</param>
    /// <returns>The value.</returns>
    public static VariantValue GetOrDefault(this IInputConfigStorage configStorage, string key,
        VariantValue @default = default)
    {
        if (!configStorage.Has(key))
        {
            return @default;
        }
        return configStorage.Get(key);
    }
}
