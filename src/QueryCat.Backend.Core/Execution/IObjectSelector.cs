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
    /// <returns>Object found by property name.</returns>
    ObjectSelectorContext.Token? SelectByProperty(ObjectSelectorContext context, string propertyName);

    /// <summary>
    /// Get the next object by index property.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="indexes">Indexes values.</param>
    /// <returns>Object found by index name.</returns>
    ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, params object?[] indexes);

    /// <summary>
    /// Set property value by property info or index to the last token.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="newValue">New value.</param>
    /// <returns><c>True</c> if property was set, <c>false</c> otherwise.</returns>
    bool SetValue(ObjectSelectorContext context, object? newValue);
}
