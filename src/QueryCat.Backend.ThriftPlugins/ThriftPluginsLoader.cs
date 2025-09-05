using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Client.Remote;
using DataType = QueryCat.Backend.Core.Types.DataType;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Load plugins using Thrift protocol.
/// </summary>
public sealed partial class ThriftPluginsLoader : PluginsLoader, IDisposable
{
    private const string FunctionsCacheFileExtension = ".fcache.json";

    private readonly IExecutionThread _thread;
    private readonly string? _applicationDirectory;
    private readonly bool _debugMode;
    private readonly LogLevel _minLogLevel;
    private readonly ThriftPluginsServer _server;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsLoader));
    private readonly HashSet<string> _loadedPlugins = new();
    private readonly string _functionsCacheDirectory;

    // Lazy loading.
    private readonly Dictionary<string, string> _fileTokenMap = new(); // file-token.
    private readonly Dictionary<string, ThriftPluginContext> _tokenContextMap = new(); // token-context.
    private readonly HashSet<string> _filesWithLoadedFunctions = new(); // List of plugins with loaded pre-cached functions.

    /// <summary>
    /// Skip actual plugins loading. For debug purposes.
    /// </summary>
    public bool SkipPluginsExecution { get; set; }

    /// <summary>
    /// Force use the specific authentication token.
    /// </summary>
    public string ForceRegistrationToken { get; set; } = string.Empty;

    /// <summary>
    /// The using server pipe name.
    /// </summary>
    public string ServerPipeName { get; } = ThriftEndpoint.GenerateIdentifier("qcath");

    internal sealed record FunctionsCache(
        [property:JsonPropertyName("createdAt")] long CreatedAt,
        [property:JsonPropertyName("functions")] List<PluginContextFunction> Functions);

    private class FunctionCallPluginBase
    {
        public string FunctionName { get; set; } = string.Empty;

        internal static async ValueTask<Core.Types.VariantValue> FunctionDelegateCallAsync(
            IExecutionThread thread,
            string functionName,
            ThriftPluginContext context,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(functionName, nameof(functionName));

            var arguments = thread.Stack.Select(SdkConvert.Convert).ToList();
            using var session = await context.GetSessionAsync(cancellationToken);
            var rawValue = await session.ClientProxy.CallFunctionAsync(0, functionName, arguments, -1, cancellationToken);
            var result = SdkConvert.Convert(rawValue);
            if (result.Type == DataType.Object && result.AsObjectUnsafe is RemoteObject remoteObject)
            {
                var sessionProvider = new ServerThriftSessionProvider(context);
                var obj = await RemoteObjectUtils.ToLocalObjectAsync(remoteObject,
                    sessionProvider, context.ObjectsStorage, cancellationToken: cancellationToken);
                if (obj != null)
                {
                    context.ObjectsStorage.Add(obj);
                    return Core.Types.VariantValue.CreateFromObject(obj);
                }
            }
            return result;
        }
    }

    private sealed class FunctionCallPluginWrapper : FunctionCallPluginBase
    {
        private readonly ThriftPluginContext _pluginContext;

        public FunctionCallPluginWrapper(ThriftPluginContext pluginContext)
        {
            _pluginContext = pluginContext;
        }

        internal ValueTask<Core.Types.VariantValue> FunctionDelegateCallAsync(IExecutionThread thread, CancellationToken cancellationToken)
            => FunctionCallPluginBase.FunctionDelegateCallAsync(thread, FunctionName, _pluginContext, cancellationToken);
    }

    /// <summary>
    /// The wrapper that loads plugin only on function call.
    /// </summary>
    private sealed class FunctionCallPluginWrapperLazy : FunctionCallPluginBase
    {
        private readonly ThriftPluginsLoader _loader;
        private readonly string _pluginFile;

        public FunctionCallPluginWrapperLazy(ThriftPluginsLoader loader, string pluginFile)
        {
            _loader = loader;
            _pluginFile = pluginFile;
        }

        internal ValueTask<Core.Types.VariantValue> FunctionDelegateCallAsync(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var pluginContext = _loader.LoadPlugin(_pluginFile);
            return FunctionCallPluginBase.FunctionDelegateCallAsync(thread, FunctionName, pluginContext, cancellationToken);
        }
    }

    public ThriftPluginsLoader(
        IExecutionThread thread,
        IEnumerable<string> pluginDirectories,
        ThriftEndpoint endpoint,
        string? applicationDirectory = null,
        string? functionsCacheDirectory = null,
        bool debugMode = false,
        LogLevel minLogLevel = LogLevel.Information,
        int maxConnectionsToPlugin = 1) : base(pluginDirectories)
    {
        _thread = thread;
        _applicationDirectory = applicationDirectory;
        _debugMode = debugMode;
        _minLogLevel = minLogLevel;
        if (!string.IsNullOrEmpty(endpoint.NamedPipe))
        {
            ServerPipeName = endpoint.NamedPipe;
        }
        _server = new ThriftPluginsServer(thread, endpoint, maxConnectionsToClient: maxConnectionsToPlugin);
        if (_debugMode)
        {
            _server.PluginRegistrationTimeoutSeconds = 100;
            _server.SkipTokenVerification = true;
            _server.Start();
        }
        _server.OnPluginRegistration += OnPluginRegistration;
        _functionsCacheDirectory = functionsCacheDirectory ?? string.Empty;
    }

    private void OnPluginRegistration(object? sender, ThriftPluginsServer.PluginRegistrationEventArgs e)
    {
        if (_tokenContextMap.TryGetValue(e.RegistrationToken, out var context))
        {
            context.Dispose();
        }
        _tokenContextMap[e.RegistrationToken] = e.PluginContext;

        var file = GetFileByContext(e.PluginContext);
        if (!_filesWithLoadedFunctions.Contains(file))
        {
            RegisterFunctions(_thread.FunctionsManager, e.PluginContext);
            _filesWithLoadedFunctions.Add(file);
        }

        try
        {
            CacheFunctions(e.PluginContext, file);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cannot write functions cache.");
        }
    }

    /// <inheritdoc />
    public override Task<int> LoadAsync(PluginsLoadingOptions options, CancellationToken cancellationToken = default)
    {
        var loadedPlugins = new List<string>();

        foreach (var pluginFile in GetPluginFiles(options))
        {
            if (IsMatchPlatform(pluginFile))
            {
                LoadPluginLazy(pluginFile, cancellationToken);
                loadedPlugins.Add(pluginFile);
            }
        }

        if (_debugMode && !string.IsNullOrEmpty(ForceRegistrationToken) && !_loadedPlugins.Any())
        {
            _logger.LogDebug("Waiting for any plugin registration.");
            _server.SetRegistrationToken(ForceRegistrationToken, ".plugin");
            _server.WaitForPluginRegistration(ForceRegistrationToken, cancellationToken);
        }

        return Task.FromResult(loadedPlugins.Count);
    }

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file)
    {
        // Base verification.
        if (!base.IsCorrectPluginFile(file))
        {
            return false;
        }

        // Executable or library.
        if (IsLibrary(file) || IsExecutable(file) || IsNugetPackage(file))
        {
            return true;
        }

        return false;
    }

    private static bool IsExecutable(string pluginFile)
    {
        var extension = Path.GetExtension(pluginFile);

        // UNIX executable.
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            && File.GetUnixFileMode(pluginFile).HasFlag(UnixFileMode.UserExecute))
        {
            return true;
        }

        // Windows executable.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsLibrary(string pluginFile)
    {
        var extension = Path.GetExtension(pluginFile);

        // UNIX library.
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                && extension.Equals(".so", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        // OSX.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            && extension.Equals(".dylib", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        // Windows library.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsNugetPackage(string pluginFile)
    {
        var extension = Path.GetExtension(pluginFile);
        return extension.Equals(".nupkg", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsMatchPlatform(string pluginFile)
    {
        var plugin = PluginInfo.CreateFromUniversalName(pluginFile);
        // If we cannot detect platform and arch - let skip the check.
        if (plugin.Platform == Application.PlatformMulti)
        {
            return true;
        }
        if (plugin.Platform == Application.PlatformUnknown && plugin.Architecture == Application.ArchitectureUnknown)
        {
            return true;
        }
        var currentPlatform = Application.GetPlatform();
        var currentArchitecture = Application.GetArchitecture();
        return currentPlatform == plugin.Platform && currentArchitecture == plugin.Architecture;
    }

    private static string GetPluginName(string pluginFile)
    {
        var plugin = PluginInfo.CreateFromUniversalName(pluginFile);
        return plugin.Name;
    }

    private bool LoadPluginSafe(string file, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Load plugin file '{PluginFile}'.", file);
            LoadPluginLazy(file, cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Cannot load plugin file '{PluginFile}': {Error}", file, e.Message);
            return false;
        }
    }

    private void LoadPluginLazy(string file, CancellationToken cancellationToken = default)
    {
        var pluginName = GetPluginName(file);
        if (_loadedPlugins.Contains(pluginName))
        {
            return;
        }

        try
        {
            if (TryGetCachedFunctions(file, out var functions))
            {
                RegisterFunctions(_thread.FunctionsManager, file, functions);
                _filesWithLoadedFunctions.Add(file);
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error while reading cache functions.");
        }

        LoadPlugin(file, cancellationToken);
    }

    private ThriftPluginContext GetContext(string file)
    {
        var pluginContext = GetContextByFile(file);
        if (pluginContext == null)
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.CannotGetPluginContext, file));
        }
        return pluginContext;
    }

    private ThriftPluginContext LoadPlugin(string file, CancellationToken cancellationToken = default)
    {
        var pluginName = GetPluginName(file);
        if (_loadedPlugins.Contains(pluginName))
        {
            return GetContext(file);
        }

        // Start host server.
        _server.Start();

        // Create auth token and save it into temp memory.
        var registrationToken = CreateRegistrationTokenAndSave(file, pluginName);

        ThriftPluginContext pluginContext;
        try
        {
            if (IsLibrary(file))
            {
                pluginContext = LoadPluginLibrary(file, registrationToken, cancellationToken);
            }
            else if (IsNugetPackage(file))
            {
                pluginContext = LoadPluginNugetPackage(file, registrationToken, cancellationToken);
            }
            else
            {
                pluginContext = LoadPluginExecutable(file, registrationToken, [], cancellationToken: cancellationToken);
            }
        }
        catch (Exception)
        {
            RemoveRegistrationToken(registrationToken, file);
            throw;
        }

        return pluginContext;
    }

    private ThriftPluginContext LoadPluginNugetPackage(
        string file,
        string registrationToken,
        CancellationToken cancellationToken = default)
    {
        var proxyExecutable = ProxyFile.ResolveProxyFileName(_applicationDirectory);
        if (string.IsNullOrEmpty(proxyExecutable))
        {
            throw new ProxyNotFoundException(file);
        }

        _logger.LogDebug("Loading '{Assembly}' with proxy. Location: '{Location}'.", file, proxyExecutable);
        return LoadPluginExecutable(
            proxyExecutable,
            registrationToken,
            ["--assembly=" + file],
            file,
            cancellationToken);
    }

    private ThriftPluginContext LoadPluginExecutable(
        string file,
        string registrationToken,
        string[] additionalArguments,
        string? realPluginFile = null,
        CancellationToken cancellationToken = default)
    {
        string FormatParameter(string key, string value) => $"--{key}={value}";

        realPluginFile ??= file;

        // Start plugin process.
        Process? process = null;
        if (!SkipPluginsExecution)
        {
            var fileName = Path.GetFileNameWithoutExtension(realPluginFile);
            process = new Process
            {
                StartInfo =
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = file,
                }
            };
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginServerPipeParameter,
                _server.ServerEndpointUri.ToString()));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginTokenParameter, registrationToken));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginParentPidParameter,
                Process.GetCurrentProcess().Id.ToString()));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginLogLevelParameter,
                _minLogLevel.ToString()));
            foreach (var arg in additionalArguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }
            process.OutputDataReceived += (_, args) => LogPluginStdOut(fileName, args.Data);
            process.ErrorDataReceived += (_, args) => LogPluginStdErr(file, args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        // Wait when plugin is loaded and it calls RegisterPlugin method.
        try
        {
            _server.WaitForPluginRegistration(registrationToken, cancellationToken);
        }
        catch (Exception)
        {
            process?.Close();
            throw;
        }

        // Plugin has been loaded.
        var pluginName = GetPluginName(realPluginFile);
        _loadedPlugins.Add(pluginName);

        // If we load .exe or .so plugin, we don't need pluginFile arg.
        // It is needed only if we load plugin using the proxy.
        return GetContext(realPluginFile);
    }

    [LoggerMessage(LogLevel.Trace, "[{PluginName}]: {Data}")]
    private partial void LogPluginStdOut(string pluginName, string? data);

    [LoggerMessage(LogLevel.Error, "[{PluginName}]: {Data}")]
    private partial void LogPluginStdErr(string pluginName, string? data);

    private ThriftPluginContext LoadPluginLibrary(
        string file,
        string registrationToken,
        CancellationToken cancellationToken = default)
    {
        var pluginName = GetPluginName(file);

        // Load library.
        var handle = nint.Zero;
        if (!SkipPluginsExecution)
        {
            handle = NativeLibrary.Load(file);
            if (!NativeLibrary.TryGetExport(handle, ThriftPluginClient.PluginMainFunctionName, out var mainAddress))
            {
                throw new PluginException(Resources.Errors.CannotGetLibraryAddress);
            }

            // Call DLL plugin method.
            var mainFunction = Marshal.GetDelegateForFunctionPointer<QueryCatPluginMainDelegate>(mainAddress);
            var args = new QueryCatPluginArguments
            {
                ServerEndpoint = Marshal.StringToHGlobalAuto(_server.ServerEndpointUri.ToString()),
                Token = Marshal.StringToHGlobalAuto(registrationToken),
                LogLevel = Marshal.StringToHGlobalAuto(_minLogLevel.ToString()),
            };
            var pluginThread = new Thread(() =>
            {
                mainFunction.Invoke(args);
            })
            {
                Name = pluginName,
                IsBackground = true,
            };
            pluginThread.Start();
        }

        // Wait when plugin is loaded and it calls RegisterPlugin method.
        try
        {
            _server.WaitForPluginRegistration(registrationToken, cancellationToken);
        }
        catch (Exception)
        {
            if (handle != nint.Zero)
            {
                NativeLibrary.Free(handle);
            }
            throw;
        }

        // Plugin has been loaded.
        _loadedPlugins.Add(pluginName);

        var context = GetContext(file);
        context.LibraryHandle = handle;

        return context;
    }

    private string CreateRegistrationTokenAndSave(string pluginFile, string pluginName)
    {
        var registrationToken = !string.IsNullOrEmpty(ForceRegistrationToken)
            ? ForceRegistrationToken
            : Guid.NewGuid().ToString("N");
        _server.SetRegistrationToken(registrationToken, pluginName);
        _fileTokenMap.Add(pluginFile, registrationToken);
        return registrationToken;
    }

    private void RemoveRegistrationToken(string registrationToken, string pluginFile)
    {
        _server.RemoveRegistrationToken(registrationToken);
        _fileTokenMap.Remove(pluginFile);
    }

    private void RegisterFunctions(IFunctionsManager functionsManager, string file,
        IEnumerable<PluginContextFunction> functions)
    {
        foreach (var function in functions)
        {
            var wrapper = new FunctionCallPluginWrapperLazy(this, file);
            var internalFunction = functionsManager.Factory.CreateFromSignature(
                function.Signature,
                wrapper.FunctionDelegateCallAsync,
                new FunctionMetadata
                {
                    Description = function.Description,
                    IsSafe = function.IsSafe,
                    IsAggregate = function.IsAggregate,
                    Formatters = function.Formatters ?? [],
                });
            functionsManager.RegisterFunction(internalFunction);
            wrapper.FunctionName = internalFunction.Name;
        }
    }

    private ThriftPluginContext? GetContextByFile(string file)
    {
        if (_fileTokenMap.TryGetValue(file, out var token) &&
            _tokenContextMap.TryGetValue(token, out var context))
        {
            return context;
        }

        return null;
    }

    private static bool TryGetKeyByValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TValue value, out TKey? key)
        where TKey : notnull
    {
        key = default;
        foreach (KeyValuePair<TKey, TValue> pair in dict)
        {
            if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
            {
                key = pair.Key;
                return true;
            }
        }
        return false;
    }

    private string GetFileByContext(ThriftPluginContext context)
    {
        if (TryGetKeyByValue(_tokenContextMap, context, out var token) && token != null)
        {
            if (TryGetKeyByValue(_fileTokenMap, token, out var file) && file != null)
            {
                return file;
            }
        }

        return string.Empty;
    }

    private void RegisterFunctions(IFunctionsManager functionsManager, ThriftPluginContext pluginContext)
    {
        foreach (var function in pluginContext.Functions)
        {
            var wrapper = new FunctionCallPluginWrapper(pluginContext);
            var internalFunction = functionsManager.Factory.CreateFromSignature(
                function.Signature,
                wrapper.FunctionDelegateCallAsync,
                new FunctionMetadata
                {
                    Description = function.Description,
                    IsSafe = function.IsSafe,
                    IsAggregate = function.IsAggregate,
                });
            functionsManager.RegisterFunction(internalFunction);
            wrapper.FunctionName = internalFunction.Name;
        }
    }

    #region Cache

    private void CacheFunctions(ThriftPluginContext context, string fileName)
    {
        if (string.IsNullOrEmpty(_functionsCacheDirectory))
        {
            return;
        }

        var fileInfo = new FileInfo(fileName);
        var cacheEntry = new FunctionsCache(fileInfo.CreationTimeUtc.Ticks, context.Functions);
        var pluginName = PluginInfo.CreateFromUniversalName(fileName).Name;
        var cacheFile = Path.Combine(_functionsCacheDirectory, pluginName + FunctionsCacheFileExtension);
        Directory.CreateDirectory(_functionsCacheDirectory);
        using var cacheFileStream = File.Create(cacheFile);
        JsonSerializer.Serialize(cacheFileStream, cacheEntry, SourceGenerationContext.Default.FunctionsCache);
    }

    private bool TryGetCachedFunctions(string fileName, out IEnumerable<PluginContextFunction> functions)
    {
        functions = [];

        if (string.IsNullOrEmpty(_functionsCacheDirectory))
        {
            return false;
        }

        var pluginName = PluginInfo.CreateFromUniversalName(fileName).Name;
        var cacheFile = Path.Combine(_functionsCacheDirectory, pluginName + FunctionsCacheFileExtension);
        if (!File.Exists(cacheFile))
        {
            return false;
        }
        var fileInfo = new FileInfo(fileName);

        using var cacheFileStream = File.OpenRead(cacheFile);
        var cacheEntry = JsonSerializer.Deserialize(cacheFileStream, SourceGenerationContext.Default.FunctionsCache);
        cacheFileStream.Close();
        if (cacheEntry == null)
        {
            return false;
        }

        // If plugin file has changed - invalidate the cache.
        if (cacheEntry.CreatedAt != fileInfo.CreationTimeUtc.Ticks)
        {
            _logger.LogDebug("Update plugin cache '{PluginName}'.", pluginName);
            File.Delete(cacheFile);
            return false;
        }

        functions = cacheEntry.Functions;
        return true;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        _server.OnPluginRegistration -= OnPluginRegistration;
        _server.Dispose();
    }
}
