using System.Reflection;

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
    ObjectSelectorContext.SelectInfo? SelectByProperty(ObjectSelectorContext context, string propertyName);

    /// <summary>
    /// Get the next object by index property.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="indexes">Indexes values.</param>
    ObjectSelectorContext.SelectInfo? SelectByIndex(ObjectSelectorContext context, object?[] indexes);

    /// <summary>
    /// Set property value by property info.
    /// </summary>
    /// <param name="obj">Object that owns the property.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="propertyInfo">Property info.</param>
    /// <param name="indexes">Indexes values.</param>
    void SetValue(object obj, object? newValue, PropertyInfo propertyInfo, object?[] indexes);
}
