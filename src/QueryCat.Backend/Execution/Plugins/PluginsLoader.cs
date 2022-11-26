using System.IO.Compression;
using System.Reflection;
using QueryCat.Backend.Logging;

namespace QueryCat.Backend.Execution.Plugins;

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

        var assembliesList = new List<Assembly>();
        foreach (var pluginFile in PluginsManager.GetPluginFiles(_pluginDirectories))
        {
            Logger.Instance.Debug($"Load plugin assembly '{pluginFile}'.");
            var extension = Path.GetExtension(pluginFile);
            Assembly? assembly = null;
            if (extension.Equals(".dll"))
            {
                assembly = LoadDll(pluginFile);
            }
            else if (extension.Equals(".nupkg"))
            {
                assembly = LoadNuget(pluginFile);
            }
            if (assembly == null)
            {
                Logger.Instance.Warning($"Cannot load from {pluginFile}.");
                continue;
            }

            assembliesList.Add(assembly);
        }

        return assembliesList;
    }

    private Assembly LoadDll(string file)
    {
        var directory = Path.GetDirectoryName(file);
        if (directory == null)
        {
            throw new InvalidOperationException($"Cannot get plugin directory by file '{file}'.");
        }

        Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var directoryInfo = new DirectoryInfo(directory);
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                if (assemblyName.Name!.Equals(fileNameWithoutExt, StringComparison.Ordinal))
                {
                    return Assembly.LoadFrom(fileInfo.FullName);
                }
            }
            return null;
        }

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        try
        {
            return Assembly.LoadFrom(file);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        }
    }

    private Assembly LoadNuget(string file)
    {
        var zip = ZipFile.OpenRead(file);

        Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            foreach (var fileInfo in zip.Entries)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                if (assemblyName.Name!.Equals(fileNameWithoutExt, StringComparison.Ordinal))
                {
                    using var stream = fileInfo.Open();
                    return LoadFromStream(stream);
                }
            }
            return null;
        }

        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        try
        {
            var pluginDll = zip.Entries.FirstOrDefault(
                f => Path.GetExtension(f.FullName).Equals(".dll")
                    && f.FullName.Contains("Plugin"));
            if (pluginDll == null)
            {
                throw new InvalidOperationException($"Cannot find plugin dll in {file}.");
            }
            using var stream = pluginDll.Open();
            var assembly = LoadFromStream(stream);
            assembly.GetTypes(); // Force assembly dependencies load.
            return assembly;
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            zip.Dispose();
        }
    }

    private static Assembly LoadFromStream(Stream stream)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Assembly.Load(ms.GetBuffer());
    }
}
