using System.IO.Compression;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.AssemblyPlugins;

/// <summary>
/// Plugins loader.
/// </summary>
public sealed class DotNetAssemblyPluginsLoader : PluginsLoader
{
    private const string DllExtension = ".dll";
    private const string NuGetExtensions = ".nupkg";

    private const string RegistrationClassName = "Registration";
    private const string RegistrationMethodName = "RegisterFunctions";

    private readonly Dictionary<string, byte[]> _rawAssembliesCache = new();
    private readonly Dictionary<string, Assembly> _loadedAssembliesCache = new();
    private readonly IFunctionsManager _functionsManager;
    private readonly HashSet<Assembly> _loadedAssemblies = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DotNetAssemblyPluginsLoader));

    public IEnumerable<Assembly> LoadedAssemblies => _loadedAssemblies;

    public DotNetAssemblyPluginsLoader(IFunctionsManager functionsManager, IEnumerable<string> pluginDirectories) : base(pluginDirectories)
    {
        _functionsManager = functionsManager;
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
    }

    private Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        _logger.LogDebug("Try to resolve assembly '{Assembly}'.", args.Name);
        var assemblyName = new AssemblyName(args.Name);
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        if (_loadedAssembliesCache.TryGetValue(assemblyName.Name, out var assembly))
        {
            _logger.LogDebug("Resolved with loaded assemblies cache.");
            return assembly;
        }
        if (_rawAssembliesCache.TryGetValue(assemblyName.Name, out var bytes))
        {
            assembly = Assembly.Load(bytes);
            _loadedAssembliesCache[assemblyName.Name] = assembly;
            _rawAssembliesCache.Remove(assemblyName.Name);
            _logger.LogDebug("Resolved with raw assemblies cache.");
            return assembly;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Cannot find assembly '{Assembly}'!", args.Name);
            _logger.LogDebug(DumpAssemblies());
        }
        return null;
    }

    private string DumpAssemblies()
    {
        var sb = new StringBuilder();
        foreach (var assembly in _loadedAssembliesCache)
        {
            sb.AppendLine($"Cache: {assembly.Key}");
        }
        foreach (var assembly in _rawAssembliesCache)
        {
            sb.AppendLine($"Raw: {assembly.Key}");
        }
        return sb.ToString();
    }

    /// <inheritdoc />
    public override Task<string[]> LoadAsync(CancellationToken cancellationToken = default)
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

        var loadedPlugins = _loadedAssemblies
            .Select(a => a.FullName ?? string.Empty)
            .Where(a => !string.IsNullOrEmpty(a))
            .ToArray();
        return Task.FromResult(loadedPlugins);
    }

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file)
    {
        // Base verification.
        if (!base.IsCorrectPluginFile(file))
        {
            return false;
        }

        var fileName = Path.GetFileName(file);

        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(DllExtension, StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(NuGetExtensions, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void RegisterFunctions(IFunctionsManager functionsManager)
    {
        foreach (var pluginAssembly in _loadedAssemblies)
        {
            RegisterFromAssembly(pluginAssembly);
        }
    }

    public void RegisterFromAssembly(Assembly assembly)
    {
        // If there is class Registration with RegisterFunctions method - call it instead. Use reflection otherwise.
        // Fast path.
        var registerType = assembly.GetType(assembly.GetName().Name + $".{RegistrationClassName}");
        if (registerType != null)
        {
            var registerMethod = registerType.GetMethod(RegistrationMethodName);
            if (registerMethod != null)
            {
                _logger.LogDebug($"Register using '{RegistrationClassName}' class.");
                _functionsManager.RegisterFactory(fm =>
                {
                    registerMethod.Invoke(null, [fm]);
                });
                return;
            }
        }

        // Get all types via reflection and try to register. Slow path.
        _logger.LogDebug("Register using types search method.");
        foreach (var type in assembly.GetTypes())
        {
            _functionsManager.RegisterFromType(type);
        }
    }

    private Assembly LoadDll(string file)
    {
        var directory = Path.GetDirectoryName(file);
        if (directory == null)
        {
            throw new PluginException(string.Format(Resources.Errors.CannotGetFileDirectory, file));
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
                throw new InvalidOperationException(string.Format(Resources.Errors.CannotFindPluginDll, file));
            }

            // Preload dependent libraries.
            var pluginDllFileName = Path.GetFileName(pluginDll.Name);
            foreach (var fileInfo in zip.Entries)
            {
                if (Path.GetExtension(fileInfo.Name).Equals(DllExtension, StringComparison.OrdinalIgnoreCase)
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
        _logger.LogTrace("Cached '{Assembly}'.", fileName);
        _rawAssembliesCache[fileName] = ms.GetBuffer();
    }

    private static Assembly LoadFromStream(Stream stream)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Assembly.Load(ms.GetBuffer());
    }
}
