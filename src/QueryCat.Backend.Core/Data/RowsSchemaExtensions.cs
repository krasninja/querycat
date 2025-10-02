namespace QueryCat.Backend.Core.Data;

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
    {
        for (var i = 0; i < schema.Columns.Length; i++)
        {
            if (Column.NameEquals(schema.Columns[i], name, sourceName))
            {
                return i;
            }
        }
        return -1;
    }

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

    /// <summary>
    /// Compares two schemas. It makes sure that tho schema can be combined. The only validation conditions
    /// for that is columns types equality and columns count equality.
    /// </summary>
    /// <param name="schema">Source schema.</param>
    /// <param name="columns">Target schema columns.</param>
    /// <returns>Returns <c>true</c> if schemas are equal, <c>false</c> otherwise.</returns>
    public static bool IsSchemaEqual(this IRowsSchema schema, Column[] columns)
    {
        if (schema.Columns.Length != columns.Length)
        {
            return false;
        }

        for (var i = 0; i < schema.Columns.Length; i++)
        {
            var sourceColumn = schema.Columns[i];
            if (sourceColumn.DataType != columns[i].DataType)
            {
                return false;
            }
        }

        return true;
    }
}
