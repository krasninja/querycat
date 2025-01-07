namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The external provider of data. It can be files (CSV, logs), cloud providers, SSH/FTP
/// links, etc.
/// </summary>
public interface IRowsSource
{
    /// <summary>
    /// Query context.
    /// </summary>
    QueryContext QueryContext { get; set; }

    /// <summary>
    /// Initialize rows output for reading or writing. If it is used for writing
    /// it should prepare all necessary data (handles, connections) to be able
    /// to write rows. As for reading, it should initialize Columns.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Awaitable task.</returns>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Close source.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Awaitable task.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the input or output to its initial position.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Awaitable task.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
