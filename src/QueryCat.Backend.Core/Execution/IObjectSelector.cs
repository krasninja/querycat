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
    ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, object?[] indexes);

    /// <summary>
    /// Set property value by property info.
    /// </summary>
    /// <param name="token">Select info with property info.</param>
    /// <param name="owner">The owner of the token property info.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="indexes">Indexes values.</param>
    /// <returns><c>True</c> if property was set, <c>false</c> otherwise.</returns>
    bool SetValue(in ObjectSelectorContext.Token token, object owner, object? newValue, object?[] indexes);
}
