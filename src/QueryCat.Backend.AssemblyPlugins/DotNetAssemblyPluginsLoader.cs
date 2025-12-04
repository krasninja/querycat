using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.AssemblyPlugins;

/// <summary>
/// Plugins loader that loads .NET assemblies.
/// </summary>
public class DotNetAssemblyPluginsLoader : PluginsLoader, IDisposable
{
    private const string DllExtension = ".dll";
    private const string NuGetExtensions = ".nupkg";

    private const string RegistrationClassName = "Registration";
    private const string RegistrationMethodName = "RegisterFunctions";
    private const string OnLoadMethodName = "OnLoadAsync";
    private const string OnUnloadMethodName = "OnUnloadAsync";

    private sealed record AssemblyContext(
        Stream Stream,
        IPluginLoadStrategy PluginLoadStrategy,
        string PluginName) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            Stream.Dispose();
        }
    }

    private readonly Dictionary<string, AssemblyContext> _rawAssembliesCache = new(); // Not loaded yet assemblies.
    private readonly Dictionary<string, Assembly> _loadedFromCacheAssemblies = new(); // Assemblies loaded from cache.
    private readonly IFunctionsManager _functionsManager;
    private readonly IExecutionThread _executionThread;
    private readonly HashSet<Assembly> _loadedAssemblies = new(); // All loaded plugin DLLs.
    private readonly HashSet<string> _domainLoadedAssemblies;
    private readonly Queue<MethodBase> _loadMethodsQueue = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DotNetAssemblyPluginsLoader));

    private static readonly string[] _monikerDirectories =
    [
#if NET10_0_OR_GREATER
        "net10.0",
#endif
#if NET9_0_OR_GREATER
        "net9.0",
#endif
#if NET8_0_OR_GREATER
        "net8.0",
#endif
#if NET7_0_OR_GREATER
        "net7.0",
#endif
#if NET6_0_OR_GREATER
        "net6.0",
#endif
        "netstandard2.1",
    ];

    /// <summary>
    /// Loaded plugins assemblies.
    /// </summary>
    public IEnumerable<Assembly> LoadedAssemblies => _loadedAssemblies;

    public DotNetAssemblyPluginsLoader(
        IFunctionsManager functionsManager,
        IExecutionThread executionThread,
        IEnumerable<string> pluginDirectories) : base(pluginDirectories)
    {
        _functionsManager = functionsManager;
        _executionThread = executionThread;
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

        _domainLoadedAssemblies =
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => Path.GetFileName(a.GetName().Name ?? string.Empty))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();
    }

    public DotNetAssemblyPluginsLoader(
        IFunctionsManager functionsManager,
        IEnumerable<string> pluginDirectories) : this(functionsManager, NullExecutionThread.Instance, pluginDirectories)
    {
    }

    private Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        _logger.LogDebug("Try to resolve assembly '{Assembly}'.", args.Name);
        var assemblyName = new AssemblyName(args.Name);
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        if (_loadedFromCacheAssemblies.TryGetValue(assemblyName.Name, out var assembly))
        {
            _logger.LogDebug("Resolved with loaded assemblies cache.");
            return assembly;
        }
        if (_rawAssembliesCache.TryGetValue(assemblyName.Name, out var context))
        {
            assembly = new PluginAssemblyLoadContext(context.PluginLoadStrategy, context.PluginName)
                .LoadFromStream(context.Stream);
            _rawAssembliesCache.Remove(assemblyName.Name);
            context.Dispose();
            _loadedFromCacheAssemblies[assemblyName.Name] = assembly;
            _logger.LogDebug("Resolved with raw assemblies cache.");
            return assembly;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Cannot find assembly '{Assembly}', skipped.", args.Name);
            _logger.LogDebug(DumpAssemblies());
        }
        return null;
    }

    public string DumpAssemblies()
    {
        var sb = new StringBuilder();
        foreach (var assembly in _loadedFromCacheAssemblies)
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
    public override async Task<int> LoadAsync(PluginsLoadingOptions options, CancellationToken cancellationToken = default)
    {
        var loadedCount = 0;

        foreach (var pluginFile in GetPluginFiles(options))
        {
            _logger.LogDebug("Load plugin file '{PluginFile}'.", pluginFile);
            var strategies = GetLoadStrategies(pluginFile);
            foreach (var strategy in strategies)
            {
                var assembly = await LoadWithStrategyAsync(strategy, cancellationToken);
                if (assembly != null)
                {
                    _loadedAssemblies.Add(assembly);
                    _logger.LogDebug("Loaded plugin target '{PluginFile}' with strategy {Strategy}.", pluginFile, strategy.GetType().Name);
                    loadedCount++;
                    break;
                }
            }
        }

        await RegisterFunctionsAsync(cancellationToken);
        if (!options.SkipLoadingCallbackCall)
        {
            await CallOnLoadAsync(cancellationToken);
        }
        return loadedCount;
    }

    public async Task CallOnLoadAsync(CancellationToken cancellationToken = default)
    {
        while (_loadMethodsQueue.TryDequeue(out var loadMethod))
        {
            var taskObject = loadMethod.Invoke(null, [_executionThread, cancellationToken]) as Task;
            if (taskObject != null)
            {
                await taskObject;
            }
        }
    }

    private async Task<Assembly?> LoadWithStrategyAsync(IPluginLoadStrategy strategy, CancellationToken cancellationToken)
    {
        // Find target framework directory.
        var monikerRoot = await FindTargetFrameworkDirectoryAsync(strategy, cancellationToken);

        // Get DLL files.
        var dllFiles = (await strategy.GetAllFilesAsync(cancellationToken))
            .Where(f => f.StartsWith(monikerRoot)
                        && Path.GetExtension(f).Equals(DllExtension, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        // Find plugin library.
        var pluginDll = Array.Find(dllFiles, f => Path.GetFileNameWithoutExtension(f)
            .Contains(PluginKeyword, StringComparison.InvariantCultureIgnoreCase));
        if (pluginDll == null)
        {
            return null;
        }

        // Preload dependent libraries.
        var pluginDllFileName = Path.GetFileNameWithoutExtension(pluginDll);
        var pluginDllFileDirectory = Path.GetDirectoryName(pluginDll);
        foreach (var file in dllFiles)
        {
            var libraryFile = file;

            var fileName = Path.GetFileNameWithoutExtension(libraryFile);
            if (_domainLoadedAssemblies.Contains(fileName)
                || _rawAssembliesCache.ContainsKey(fileName)
                || Path.GetFileNameWithoutExtension(libraryFile).Equals(pluginDllFileName)
                || Path.GetDirectoryName(libraryFile) != pluginDllFileDirectory)
            {
                continue;
            }

            var runtimeSpecificLibraryFile = await GetTfmRuntimeSpecificPackageFileAsync(libraryFile, strategy, cancellationToken);
            if (!string.IsNullOrEmpty(runtimeSpecificLibraryFile))
            {
                libraryFile = runtimeSpecificLibraryFile;
            }

            await using var libraryFileStream = await strategy.GetFileAsync(libraryFile, cancellationToken);
            var stream = await CloneStreamAsync(libraryFileStream, cancellationToken);
            if (stream == Stream.Null)
            {
                continue;
            }
            _logger.LogTrace("Cached '{Assembly}'.", fileName);
            _rawAssembliesCache[fileName] = new AssemblyContext(stream, strategy, pluginDllFileName);
        }

        // Load plugin library.
        await using var pluginFileStream = await strategy.GetFileAsync(pluginDll, cancellationToken);
        var pluginStream = await CloneStreamAsync(pluginFileStream, cancellationToken);
        if (pluginStream == Stream.Null)
        {
            return null;
        }
        return new PluginAssemblyLoadContext(strategy, pluginDllFileName).LoadFromStream(pluginStream);
    }

    /// <summary>
    /// Some packages (for example, System.Diagnostics.EventLog) provide
    /// framework specific versions. If we load them in standard way - we get platform
    /// not supported exception. This method tries to resolve them from "runtimes" directory.
    /// </summary>
    /// <param name="libraryFile">Library file name.</param>
    /// <param name="pluginLoadStrategy">Plugin load strategy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Target framework runtime specific file.</returns>
    private static async Task<string> GetTfmRuntimeSpecificPackageFileAsync(
        string libraryFile,
        IPluginLoadStrategy pluginLoadStrategy,
        CancellationToken cancellationToken)
    {
        var libraryDirectory = Path.GetDirectoryName(libraryFile);
        var libraryFileName = Path.GetFileName(libraryFile);
        var platform = Application.GetPlatform();

        if (string.IsNullOrEmpty(libraryDirectory))
        {
            return string.Empty;
        }

        foreach (var monikerDirectory in _monikerDirectories)
        {
            // Example: ./runtimes/win/lib/net9.0 .
            var runtimePath = Path.Combine(
                libraryDirectory,
                "runtimes",
                platform,
                "lib",
                monikerDirectory,
                libraryFileName);
            if (await pluginLoadStrategy.GetFileSizeAsync(runtimePath, cancellationToken) > 0)
            {
                return runtimePath;
            }
        }

        return string.Empty;
    }

    private static IPluginLoadStrategy[] GetLoadStrategies(string target) =>
    [
        new NuGetPluginLoadStrategy(target),
        new FilePluginLoadStrategy(target),
    ];

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

    private async Task RegisterFunctionsAsync(CancellationToken cancellationToken)
    {
        foreach (var pluginAssembly in _loadedAssemblies)
        {
            await RegisterFromAssemblyAsync(pluginAssembly, cancellationToken);
        }
    }

    private async Task RegisterFromAssemblyAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        // If there is class Registration with RegisterFunctions method - call it instead. Use reflection otherwise.
        // Fast path.
        var registerType = assembly.GetType(assembly.GetName().Name + $".{RegistrationClassName}");
        if (registerType != null)
        {
            // static void RegisterFunctions(IFunctionsManager functionsManager).
            var registerMethod = registerType.GetMethod(RegistrationMethodName);
            if (registerMethod != null)
            {
                _logger.LogDebug("Register using '{ClassName}' class.", RegistrationClassName);
                registerMethod.Invoke(null, [_functionsManager]);
            }

            // static Task OnLoadAsync(IExecutionThread executionThread, CancellationToken cancellationToken).
            var loadMethod = registerType.GetMethod(OnLoadMethodName);
            if (loadMethod != null)
            {
                _loadMethodsQueue.Enqueue(loadMethod);
            }
        }
        else
        {
            // Get all types via reflection and try to register. Slow path.
            _logger.LogDebug("Register using types search method.");
            foreach (var type in assembly.GetTypes())
            {
                var functions = _functionsManager.Factory.CreateFromType(type);
                _functionsManager.RegisterFunctions(functions);
            }
        }

        await OnPluginLoadedAsync(assembly, registerType, cancellationToken);
    }

    /// <summary>
    /// The method is called after plugin load and registration.
    /// </summary>
    /// <param name="assembly">Plugin assembly.</param>
    /// <param name="registrationClassType">Registration class if available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnPluginLoadedAsync(Assembly assembly, Type? registrationClassType, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task<string> FindTargetFrameworkDirectoryAsync(IPluginLoadStrategy pluginLoadStrategy,
        CancellationToken cancellationToken)
    {
        var files = (await pluginLoadStrategy.GetAllFilesAsync(cancellationToken)).ToArray();
        foreach (var moniker in _monikerDirectories)
        {
            var monikerDirectory = "lib/" + moniker + "/";
            if (files.Any(e => e.StartsWith(monikerDirectory)))
            {
                return monikerDirectory;
            }
        }
        return string.Empty;
    }

    private static async Task<MemoryStream> CloneStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var value in _rawAssembliesCache.Values)
        {
            value.Dispose();
        }
    }
}
