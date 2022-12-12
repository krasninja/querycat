using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Abstractions;

/// <summary>
/// The external provider of data. It can be files (CSV, logs), cloud providers, SSH/FTP
/// links, etc.
/// </summary>
public interface IRowsSource
{
    /// <summary>
    /// Initialize rows output for reading or writing. If it is used for writing
    /// it should prepare all necessary data (handles, connections) to be able
    /// to write rows. As for reading, it should initialize Columns.
    /// </summary>
    void Open();

    /// <summary>
    /// Set query execution context.
    /// </summary>
    /// <param name="queryContext">Query context.</param>
    void SetContext(QueryContext queryContext);

    /// <summary>
    /// Close source.
    /// </summary>
    void Close();
}
