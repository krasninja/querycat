namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Select final object by object expression like "user.Address.Phones[0]".
/// </summary>
public interface IObjectSelector
{
    /// <summary>
    /// Get the next object by property name.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Object found by property name.</returns>
    ValueTask<ObjectSelectorContext.Token?> SelectByPropertyAsync(
        ObjectSelectorContext context,
        string propertyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next object by index property.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="indexes">Indexes values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Object found by index name.</returns>
    ValueTask<ObjectSelectorContext.Token?> SelectByIndexAsync(
        ObjectSelectorContext context,
        object?[] indexes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set property value by property info or index to the last token.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><c>True</c> if property was set, <c>false</c> otherwise.</returns>
    ValueTask<bool> SetValueAsync(
        ObjectSelectorContext context,
        object? newValue,
        CancellationToken cancellationToken = default);
}
