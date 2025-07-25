namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Provides some metadata that describes the model.
/// </summary>
public interface IModelDescription
{
    /// <summary>
    /// Logical name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description.
    /// </summary>
    string Description { get; }
}
