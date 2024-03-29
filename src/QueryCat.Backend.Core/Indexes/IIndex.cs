namespace QueryCat.Backend.Core.Indexes;

/// <summary>
/// General index interface.
/// </summary>
public interface IIndex
{
    /// <summary>
    /// Recreate the index values.
    /// </summary>
    void Rebuild();
}
