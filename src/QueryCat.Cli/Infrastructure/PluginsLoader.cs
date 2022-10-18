using System.Reflection;
using QueryCat.Backend.Logging;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Plugins loader.
/// </summary>
internal class PluginsLoader
{
    private readonly IEnumerable<string> _pluginDirectories;

    public PluginsLoader(IEnumerable<string> pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
    }

    /// <summary>
    /// Load plugins.
    /// </summary>
    /// <returns>Loaded assemblies.</returns>
    public IEnumerable<Assembly> LoadPlugins()
    {
        var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        if (string.IsNullOrEmpty(path))
        {
            return Enumerable.Empty<Assembly>();
        }

        // Loaded plugins may have dependencies which can be resolved only in their directories.
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

        var assembliesList = new List<Assembly>();
        foreach (var source in _pluginDirectories)
        {
            var pluginFiles = Directory.GetFiles(source, "*Plugin*.dll");
            foreach (var file in pluginFiles)
            {
                Logger.Instance.Debug($"Load plugin assembly '{file}'.");
                var assembly = Assembly.LoadFrom(file);
                assembliesList.Add(assembly);
            }
        }

        return assembliesList;
    }

    private Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);

        foreach (var pluginDirectory in _pluginDirectories)
        {
            var directoryInfo = new DirectoryInfo(pluginDirectory);
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                if (assemblyName.Name!.Equals(fileNameWithoutExt, StringComparison.Ordinal))
                {
                    return Assembly.LoadFrom(fileInfo.FullName);
                }
            }
        }

        return null;
    }
}
