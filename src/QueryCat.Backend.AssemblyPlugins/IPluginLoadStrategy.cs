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
    /// <returns>Files full path.</returns>
    IEnumerable<string> GetAllFiles();

    /// <summary>
    /// Get file stream.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns>Stream.</returns>
    Stream GetFile(string file);
}
