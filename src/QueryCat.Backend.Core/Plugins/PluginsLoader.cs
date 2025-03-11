namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugins loading subsystem.
/// </summary>
public abstract class PluginsLoader : IPluginsLoader
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

    /// <inheritdoc />
    public abstract Task<int> LoadAsync(PluginsLoadingOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the file can be used for the specified plugin loader.
    /// </summary>
    /// <param name="file">File.</param>
    /// <returns><c>True</c> if correct plugin file, <c>false</c> otherwise.</returns>
    public virtual bool IsCorrectPluginFile(string file)
    {
        // File name must contain "plugin" word.
        if (!File.Exists(file)
            || !Path.GetFileName(file).Contains("plugin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip debug files.
        if (file.EndsWith(".dbg"))
        {
            return false;
        }

        // The file refers to internal QueryCat file and should be skipped.
        if (IsAppSpecificFile(file))
        {
            return false;
        }

        return true;
    }

    private readonly record struct PluginInfoWithPath(PluginInfo PluginInfo, string Path)
    {
        public string PluginName => this.PluginInfo.Name;

        public Version PluginVersion => this.PluginInfo.Version;
    }

    /// <summary>
    /// Get correct plugins files.
    /// </summary>
    /// <param name="skipDuplicates">Skip duplicate files to avoid double loading.</param>
    /// <returns>Plugins files.</returns>
    protected internal IEnumerable<string> GetPluginFiles(bool skipDuplicates = true)
    {
        var plugins = new Dictionary<string, PluginInfoWithPath>();
        var files = new List<string>();

        void AddOrReplace(Dictionary<string, PluginInfoWithPath> dict, PluginInfoWithPath plugin)
        {
            // If there is duplicate we try to load the latest version only.
            if (dict.TryGetValue(plugin.PluginName, out var existing))
            {
                if (plugin.PluginVersion > existing.PluginInfo.Version)
                {
                    dict[plugin.PluginName] = plugin;
                }
            }
            else
            {
                dict[plugin.PluginName] = plugin;
            }
        }

        foreach (var pluginDirectory in PluginDirectories)
        {
            if (IsCorrectPluginFile(pluginDirectory))
            {
                var plugin = PluginInfo.CreateFromUniversalName(pluginDirectory);
                AddOrReplace(plugins, new PluginInfoWithPath(plugin, pluginDirectory));
                files.Add(pluginDirectory);
            }

            if (!Directory.Exists(pluginDirectory))
            {
                continue;
            }

            foreach (var pluginFile in Directory.EnumerateFiles(pluginDirectory))
            {
                if (IsCorrectPluginFile(pluginFile))
                {
                    var plugin = PluginInfo.CreateFromUniversalName(pluginFile);
                    AddOrReplace(plugins, new PluginInfoWithPath(plugin, pluginFile));
                    files.Add(pluginFile);
                }
            }
        }

        return skipDuplicates ? plugins.Values.Select(v => v.Path) : files;
    }

    /// <summary>
    /// Sometimes application specific file can be considered as plugin file.
    /// We must skip them.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns><c>True</c> if this is application specific file, <c>false</c> otherwise.</returns>
    private static bool IsAppSpecificFile(string file)
    {
        var fileName = Path.GetFileName(file);
        return fileName.StartsWith("QueryCat.Plugins.Client")
            || fileName.StartsWith("QueryCat.Plugins.Sdk")
            || fileName.StartsWith("QueryCat.Backend")
            || fileName.StartsWith("QueryCat.Backend.AssemblyPlugins")
            || fileName.StartsWith("qcat-plugins-proxy");
    }
}
