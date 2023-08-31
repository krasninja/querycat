namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The interfaces identifies that current iterator or input has children.
/// </summary>
public interface IRowsIteratorParent
{
    /// <summary>
    /// Get children inputs or iterators.
    /// </summary>
    IEnumerable<IRowsSchema> GetChildren();
}
