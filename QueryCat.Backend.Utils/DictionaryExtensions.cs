namespace QueryCat.Backend.Utils;

/// <summary>
/// Extensions for <see cref="IDictionary{TKey, TValue}" />.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Add or update dictionary value.
    /// </summary>
    /// <param name="dictionary">Instance of <see cref="IDictionary{TKey,TValue}" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="addValueFactory">Delegate to create new value to add to dictionary.</param>
    /// <param name="updateValueFactory">Delegate to update the existing value. The existing value is ignored.</param>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <returns>New or updated value.</returns>
    public static TValue? AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue?> dictionary,
        TKey key,
        Func<TKey, TValue?> addValueFactory,
        Action<TKey, TValue?> updateValueFactory)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            updateValueFactory.Invoke(key, value);
            return value;
        }
        else
        {
            var newValue = addValueFactory.Invoke(key);
            dictionary.Add(key, newValue);
            return newValue;
        }
    }

    /// <summary>
    /// Add or update dictionary value.
    /// </summary>
    /// <param name="dictionary">Instance of <see cref="IDictionary{TKey,TValue}" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="addValueFactory">Delegate to create new value to add to dictionary.</param>
    /// <param name="updateValueFactory">Delegate to update the existing value.</param>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <returns>New or updated value.</returns>
    public static TValue? AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue?> dictionary,
        TKey key,
        Func<TKey, TValue?> addValueFactory,
        Func<TKey, TValue?, TValue?> updateValueFactory)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            var newValue = updateValueFactory.Invoke(key, value);
            dictionary[key] = newValue;
            return newValue;
        }
        else
        {
            var newValue = addValueFactory.Invoke(key);
            dictionary.Add(key, newValue);
            return newValue;
        }
    }

    /// <summary>
    /// Get the value from dictionary if exists or use value factory to generate and get.
    /// </summary>
    /// <param name="dictionary">Instance of <see cref="IDictionary{TKey,TValue}" />.</param>
    /// <param name="key">Key.</param>
    /// <param name="valueFactory">Delegate to create new value to add to dictionary.</param>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <returns>New or existing value.</returns>
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> valueFactory)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        else
        {
            var newValue = valueFactory.Invoke(key);
            dictionary.Add(key, newValue);
            return newValue;
        }
    }
}
