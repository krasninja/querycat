namespace QueryCat.Backend.AssemblyPlugins;

/// <summary>
/// Simple virtual files system for a plugin that allows to get all files
/// and open them.
/// </summary>
internal interface IPluginLoadStrategy
{
    /// <summary>
    /// Enumerate all files within the plugin.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Files full path.</returns>
    Task<IReadOnlyCollection<string>> GetAllFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file stream.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream.</returns>
    Task<Stream> GetFileAsync(string file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file size.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File size in bytes.</returns>
    Task<long> GetFileSizeAsync(string file, CancellationToken cancellationToken = default);
}
