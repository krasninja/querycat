namespace QueryCat.Backend.Abstractions.Plugins;

/// <summary>
/// Plugins loading subsystem.
/// </summary>
public abstract class PluginsLoader
{
    /// <summary>
    /// The directories to search for plugin files.
    /// </summary>
    protected IEnumerable<string> PluginDirectories { get; }

    public PluginsLoader(IEnumerable<string> pluginDirectories)
    {
        PluginDirectories = pluginDirectories;
    }

    /// <summary>
    /// Load plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public abstract Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the file can be used for the specified plugin loader.
    /// </summary>
    /// <param name="file">File.</param>
    /// <returns><c>True</c> if correct plugin file, <c>false</c> otherwise.</returns>
    public abstract bool IsCorrectPluginFile(string file);

    /// <summary>
    /// Get correct plugins files.
    /// </summary>
    /// <returns>Plugins files.</returns>
    protected internal IEnumerable<string> GetPluginFiles()
    {
        var loaded = new HashSet<string>();

        foreach (var pluginDirectory in PluginDirectories)
        {
            var fileName = Path.GetFileName(pluginDirectory);
            if (IsCorrectPluginFile(pluginDirectory)
                && !loaded.Contains(fileName))
            {
                loaded.Add(fileName);
                yield return pluginDirectory;
            }

            if (!Directory.Exists(pluginDirectory))
            {
                continue;
            }

            foreach (var pluginFile in Directory.EnumerateFiles(pluginDirectory))
            {
                fileName = Path.GetFileName(pluginFile);
                if (IsCorrectPluginFile(pluginFile) && !loaded.Contains(fileName))
                {
                    loaded.Add(fileName);
                    yield return pluginFile;
                }
            }
        }
    }
}
