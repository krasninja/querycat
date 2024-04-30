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
    ObjectSelectorContext.Token? SelectByProperty(ObjectSelectorContext context, string propertyName);

    /// <summary>
    /// Get the next object by index property.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="indexes">Indexes values.</param>
    ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, object?[] indexes);

    /// <summary>
    /// Set property value by property info.
    /// </summary>
    /// <param name="token">Select info with property info.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="indexes">Indexes values.</param>
    /// <returns><c>True</c> if property was set, <c>false</c> otherwise.</returns>
    bool SetValue(in ObjectSelectorContext.Token token, object? newValue, object?[] indexes);
}
