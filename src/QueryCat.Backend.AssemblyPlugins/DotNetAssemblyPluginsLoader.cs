using System.IO.Compression;
using System.Reflection;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.AssemblyPlugins;

/// <summary>
/// Plugins loader.
/// </summary>
public sealed class DotNetAssemblyPluginsLoader : PluginsLoader
{
    private const string DllExtension = ".dll";
    private const string NuGetExtensions = ".nupkg";

    private readonly Dictionary<string, byte[]> _rawAssembliesCache = new();
    private readonly Dictionary<string, Assembly> _loadedAssembliesCache = new();
    private readonly FunctionsManager _functionsManager;
    private readonly HashSet<Assembly> _loadedAssemblies = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<DotNetAssemblyPluginsLoader>();

    public IEnumerable<Assembly> LoadedAssemblies => _loadedAssembliesCache.Values;

    public DotNetAssemblyPluginsLoader(FunctionsManager functionsManager, IEnumerable<string> pluginDirectories) : base(pluginDirectories)
    {
        _functionsManager = functionsManager;
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

    /// <inheritdoc />
    public override Task LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (var pluginFile in GetPluginFiles())
        {
            _logger.LogDebug("Load plugin assembly '{PluginFile}'.", pluginFile);
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
                _logger.LogWarning("Cannot load from '{PluginFile}'.", pluginFile);
                continue;
            }
            _loadedAssemblies.Add(assembly);
        }

        RegisterFunctions(_functionsManager);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file)
    {
        var fileName = Path.GetFileName(file);

        if (!File.Exists(file) || !fileName.Contains("Plugin"))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fileName.StartsWith("QueryCat.Plugins.Client")
            || fileName.StartsWith("QueryCat.Plugins.Sdk")
            || fileName.StartsWith("QueryCat.Backend"))
        {
            return false;
        }

        return true;
    }

    private void RegisterFunctions(FunctionsManager functionsManager)
    {
        foreach (var pluginAssembly in _loadedAssemblies)
        {
            functionsManager.RegisterFromAssembly(pluginAssembly);
        }
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