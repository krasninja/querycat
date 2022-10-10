namespace QueryCat.Backend.Relational;

/// <summary>
/// The interface described rows schema (columns) information.
/// </summary>
public interface IRowsSchema
{
    /// <summary>
    /// Rows set columns.
    /// </summary>
    Column[] Columns { get; }
}
