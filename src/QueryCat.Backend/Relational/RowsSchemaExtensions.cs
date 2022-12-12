using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Extensions for <see cref="IRowsSchema" />.
/// </summary>
public static class RowsSchemaExtensions
{
    /// <summary>
    /// Get column index by full name.
    /// </summary>
    /// <param name="schema">Rows schema.</param>
    /// <param name="name">Column name.</param>
    /// <param name="sourceName">Optional source or prefix.</param>
    /// <returns>Found column index or -1.</returns>
    public static int GetColumnIndexByName(this IRowsSchema schema, string name, string? sourceName = null)
        => Array.FindIndex(schema.Columns, c => Column.NameEquals(c, name, sourceName));

    /// <summary>
    /// Get column index by instance of <see cref="Column" />.
    /// </summary>
    /// <param name="schema">Rows schema.</param>
    /// <param name="column">Column.</param>
    /// <returns>Found column index or -1.</returns>
    public static int GetColumnIndex(this IRowsSchema schema, Column column)
        => Array.IndexOf(schema.Columns, column);

    /// <summary>
    /// Get instance of <see cref="Column" /> or null.
    /// </summary>
    /// <param name="schema">Rows schema.</param>
    /// <param name="name">Column name.</param>
    /// <param name="sourceName">Optional source or prefix.</param>
    /// <returns>Instance of <see cref="Column" /> or null.</returns>
    public static Column? GetColumnByName(this IRowsSchema schema, string name, string? sourceName = null)
    {
        var index = GetColumnIndexByName(schema, name, sourceName);
        if (index > -1)
        {
            return schema.Columns[index];
        }
        return null;
    }
}
