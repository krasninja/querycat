using QueryCat.Backend;

namespace QueryCat.Cli.Infrastructure;

public class AppExecutionOptions : ExecutionOptions
{
#if ENABLE_PLUGINS
    /// <summary>
    /// List of directories to search for plugins.
    /// </summary>
    public List<string> PluginDirectories { get; } = new();

    /// <summary>
    /// Plugins repository. If empty - default will be used.
    /// </summary>
    public string? PluginsRepositoryUri { get; init; }
#endif
}
