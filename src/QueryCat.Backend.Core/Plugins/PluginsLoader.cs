namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugins loading subsystem.
/// </summary>
public abstract class PluginsLoader
{
    /// <summary>
    /// The directories to search for plugin files.
    /// </summary>
    protected IEnumerable<string> PluginDirectories { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="pluginDirectories">Directories.</param>
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
                && loaded.Add(fileName))
            {
                yield return pluginDirectory;
            }

            if (!Directory.Exists(pluginDirectory))
            {
                continue;
            }

            foreach (var pluginFile in Directory.EnumerateFiles(pluginDirectory))
            {
                fileName = Path.GetFileName(pluginFile);
                if (IsCorrectPluginFile(pluginFile) && loaded.Add(fileName))
                {
                    yield return pluginFile;
                }
            }
        }
    }

    /// <summary>
    /// Sometimes application specific file can be considered as plugin file.
    /// We must skip them.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns><c>True</c> if this is application specific file, <c>false</c> otherwise.</returns>
    protected static bool IsAppSpecificFile(string file)
    {
        var fileName = Path.GetFileName(file);
        return fileName.StartsWith("QueryCat.Plugins.Client")
            || fileName.StartsWith("QueryCat.Plugins.Sdk")
            || fileName.StartsWith("QueryCat.Backend")
            || fileName.StartsWith("QueryCat.Backend.AssemblyPlugins");
    }
}
