using System.IO.Compression;
using System.Reflection;
using Serilog;

namespace QueryCat.Backend.Execution.Plugins;

/// <summary>
/// Plugins loader.
/// </summary>
internal sealed class PluginsLoader
{
    private const string DllExtension = ".dll";
    private const string NuGetExtensions = ".nupkg";

    private readonly IEnumerable<string> _pluginDirectories;
    private readonly Dictionary<string, byte[]> _rawAssembliesCache = new();
    private readonly Dictionary<string, Assembly> _loadedAssembliesCache = new();

    public PluginsLoader(IEnumerable<string> pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
    }

    private Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        if (_loadedAssembliesCache.TryGetValue(assemblyName.Name, out var assembly))
        {
            return assembly;
        }
        if (_rawAssembliesCache.TryGetValue(assemblyName.Name, out var bytes))
        {
            assembly = Assembly.Load(bytes);
            _loadedAssembliesCache[assemblyName.Name] = assembly;
            _rawAssembliesCache.Remove(assemblyName.Name);
            return assembly;
        }
        return null;
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
        var loadedFiles = new HashSet<string>();
        foreach (var pluginFile in PluginsManager.GetPluginFiles(_pluginDirectories))
        {
            var fileName = GetPluginName(pluginFile);
            if (loadedFiles.Contains(fileName))
            {
                Log.Logger.Warning("Plugin assembly '{PluginFile}' has been already loaded.", pluginFile);
                continue;
            }
            loadedFiles.Add(fileName);

            Log.Logger.Debug("Load plugin assembly '{PluginFile}'.", pluginFile);
            var extension = Path.GetExtension(pluginFile);
            Assembly? assembly = null;
            if (extension.Equals(DllExtension, StringComparison.OrdinalIgnoreCase))
            {
                assembly = LoadDll(pluginFile);
            }
            else if (extension.Equals(NuGetExtensions, StringComparison.OrdinalIgnoreCase))
            {
                assembly = LoadNuget(pluginFile);
            }
            if (assembly == null)
            {
                Log.Logger.Warning("Cannot load from '{PluginFile}'.", pluginFile);
                continue;
            }

            assembliesList.Add(assembly);
        }

        return assembliesList;
    }

    private static string GetPluginName(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        return PluginsManager.GetNameFromKey(fileName);
    }

    private Assembly LoadDll(string file)
    {
        var directory = Path.GetDirectoryName(file);
        if (directory == null)
        {
            throw new PluginException($"Cannot get plugin directory by file '{file}'.");
        }

        var directoryInfo = new DirectoryInfo(directory);
        foreach (var fileInfo in directoryInfo.GetFiles())
        {
            var pluginDllFileName = fileInfo.Name;
            if (Path.GetExtension(pluginDllFileName).Equals(DllExtension, StringComparison.OrdinalIgnoreCase)
                && !fileInfo.Name.Equals(pluginDllFileName))
            {
                using var stream = File.OpenRead(fileInfo.FullName);
                CacheAssemblyFromStream(fileInfo.Name, stream);
            }
        }

        return Assembly.LoadFrom(file);
    }

    private Assembly LoadNuget(string file)
    {
        var zip = ZipFile.OpenRead(file);

        try
        {
            // Find plugin library.
            var pluginDll = zip.Entries.FirstOrDefault(
                f => Path.GetExtension(f.FullName).Equals(DllExtension, StringComparison.OrdinalIgnoreCase)
                    && f.FullName.Contains("Plugin"));
            if (pluginDll == null)
            {
                throw new InvalidOperationException($"Cannot find plugin dll in '{file}'.");
            }

            // Preload dependent libraries.
            var pluginDllFileName = Path.GetFileName(pluginDll.Name);
            foreach (var fileInfo in zip.Entries)
            {
                if (Path.GetExtension(pluginDllFileName).Equals(DllExtension, StringComparison.OrdinalIgnoreCase)
                    && !fileInfo.Name.Equals(pluginDllFileName))
                {
                    using var stream = fileInfo.Open();
                    CacheAssemblyFromStream(fileInfo.Name, stream);
                }
            }

            // Load plugin library.
            using var pluginStream = pluginDll.Open();
            var assembly = LoadFromStream(pluginStream);

            return assembly;
        }
        finally
        {
            zip.Dispose();
        }
    }

    private void CacheAssemblyFromStream(string fileName, Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        fileName = Path.GetFileNameWithoutExtension(fileName);
        _rawAssembliesCache[fileName] = ms.GetBuffer();
    }

    private static Assembly LoadFromStream(Stream stream)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Assembly.Load(ms.GetBuffer());
    }
}
