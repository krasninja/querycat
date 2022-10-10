using System.Reflection;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Plugins loader.
/// </summary>
public class PluginsLoader
{
    private readonly IEnumerable<string> _pluginDirectories;

    public PluginsLoader(IEnumerable<string> pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
    }

    public IEnumerable<Assembly> GetPlugins()
    {
        var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        if (string.IsNullOrEmpty(path))
        {
            return Enumerable.Empty<Assembly>();
        }

        // Loaded plugins may have dependencies which can be resolved only in their directories.
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

        var assembliesList = new List<Assembly>();
        foreach (var source in _pluginDirectories.Concat(new[] { path }))
        {
            var pluginFiles = Directory.GetFiles(source, "*Plugin*.dll");
            foreach (var file in pluginFiles)
            {
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
