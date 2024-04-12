using QueryCat.Backend.Core.Types;

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
    void PushObjectByProperty(ObjectSelectorContext context, string propertyName);

    /// <summary>
    /// Get the next object by index property.
    /// </summary>
    /// <param name="context">Selector context.</param>
    /// <param name="indexes">Indexes values.</param>
    void PushObjectByIndex(ObjectSelectorContext context, VariantValue[] indexes);
}
