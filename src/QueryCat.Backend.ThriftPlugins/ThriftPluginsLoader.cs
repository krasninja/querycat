using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Load plugins using Thrift protocol.
/// </summary>
public sealed class ThriftPluginsLoader : PluginsLoader, IDisposable
{
    private const string FunctionsCacheFileExtension = ".fcache.json";

    private readonly IExecutionThread _thread;
    private readonly bool _debugMode;
    private readonly LogLevel _minLogLevel;
    private readonly ThriftPluginsServer _server;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsLoader));
    private readonly HashSet<string> _loadedPlugins = new();
    private readonly string _functionsCacheDirectory;

    // Lazy loading.
    private readonly Dictionary<string, string> _fileTokenMap = new(); // file-token.
    private readonly Dictionary<string, ThriftPluginsServer.PluginContext> _tokenContextMap = new(); // token-context.
    private readonly HashSet<string> _filesWithLoadedFunctions = new(); // List of plugins with loaded pre-cached functions.

    /// <summary>
    /// Skip actual plugins loading. For debug purposes.
    /// </summary>
    public bool SkipPluginsExecution { get; set; }

    /// <summary>
    /// Force use the specific authentication token.
    /// </summary>
    public string ForceAuthToken { get; set; } = string.Empty;

    /// <summary>
    /// The using server pipe name.
    /// </summary>
    public string ServerPipeName { get; } = "qcat-" + Guid.NewGuid().ToString("N");

    internal sealed record FunctionsCache(
        [property:JsonPropertyName("createdAt")] long CreatedAt,
        [property:JsonPropertyName("functions")] List<PluginContextFunction> Functions);

    public ThriftPluginsLoader(
        IExecutionThread thread,
        IEnumerable<string> pluginDirectories,
        ThriftPluginsServer.TransportType transportType = ThriftPluginsServer.TransportType.NamedPipes,
        string? serverPipeName = null,
        string? functionsCacheDirectory = null,
        bool debugMode = false,
        LogLevel minLogLevel = LogLevel.Information) : base(pluginDirectories)
    {
        _thread = thread;
        _debugMode = debugMode;
        _minLogLevel = minLogLevel;
        if (!string.IsNullOrEmpty(serverPipeName))
        {
            ServerPipeName = serverPipeName;
        }
        _server = new ThriftPluginsServer(thread, transportType, ServerPipeName);
        if (_debugMode)
        {
            _server.SkipTokenVerification = true;
            _server.Start();
        }
        _server.OnPluginRegistration += OnPluginRegistration;
        _functionsCacheDirectory = functionsCacheDirectory ?? string.Empty;
    }

    private void OnPluginRegistration(object? sender, ThriftPluginsServer.PluginRegistrationEventArgs e)
    {
        _tokenContextMap.Add(e.AuthToken, e.PluginContext);

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
    public override Task LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (var pluginFile in GetPluginFiles())
        {
            if (IsMatchPlatform(pluginFile))
            {
                LoadPluginSafe(pluginFile, cancellationToken);
            }
        }

        if (_debugMode && !string.IsNullOrEmpty(ForceAuthToken) && !_loadedPlugins.Any())
        {
            _logger.LogDebug("Waiting for any plugin registration.");
            _server.RegisterAuthToken(ForceAuthToken, ".plugin");
            _server.WaitForPluginRegistration(ForceAuthToken, cancellationToken);
        }

        return Task.CompletedTask;
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
        if (IsLibrary(file) || IsExecutable(file))
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
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) && extension.Equals(".so", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Windows library.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsMatchPlatform(string pluginFile)
    {
        var plugin = PluginInfo.CreateFromUniversalName(pluginFile);
        // If we cannot detect platform and arch - let skip the check.
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

    private void LoadPluginSafe(string file, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Load plugin file '{PluginFile}'.", file);
            LoadPluginLazy(file, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Cannot load plugin file '{PluginFile}'.", file);
            throw;
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

    private ThriftPluginsServer.PluginContext GetContext(string file)
    {
        var pluginContext = GetContextByFile(file);
        if (pluginContext == null)
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.CannotGetPluginContext, file));
        }
        return pluginContext;
    }

    private ThriftPluginsServer.PluginContext LoadPlugin(string file, CancellationToken cancellationToken = default)
    {
        var pluginName = GetPluginName(file);
        if (_loadedPlugins.Contains(pluginName))
        {
            return GetContext(file);
        }

        // Start host server.
        _server.Start();

        // Create auth token and save it into temp memory.
        var authToken = CreateAuthTokenAndSave(file, pluginName);

        if (IsLibrary(file))
        {
            return LoadPluginLibrary(file, authToken, cancellationToken);
        }
        return LoadPluginExecutable(file, authToken, cancellationToken);
    }

    private ThriftPluginsServer.PluginContext LoadPluginExecutable(
        string file,
        string authToken,
        CancellationToken cancellationToken = default)
    {
        string FormatParameter(string key, string value) => $"--{key}={value}";

        // Start plugin process.
        Process? process = null;
        if (!SkipPluginsExecution)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
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
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginServerPipeParameter, GetPipeName()));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginTokenParameter, authToken));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginParentPidParameter,
                Process.GetCurrentProcess().Id.ToString()));
            process.StartInfo.ArgumentList.Add(FormatParameter(ThriftPluginClient.PluginLogLevelParameter,
                _minLogLevel.ToString()));
            process.OutputDataReceived += (_, args) => _logger.LogTrace($"[{fileName}]: {args.Data}");
            process.ErrorDataReceived += (_, args) => _logger.LogError($"[{fileName}]: {args.Data}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        // Wait when plugin is loaded and it calls RegisterPlugin method.
        try
        {
            _server.WaitForPluginRegistration(authToken, cancellationToken);
        }
        catch (Exception)
        {
            process?.Close();
            throw;
        }

        // Plugin has been loaded.
        var pluginName = GetPluginName(file);
        _loadedPlugins.Add(pluginName);

        return GetContext(file);
    }

    private ThriftPluginsServer.PluginContext LoadPluginLibrary(
        string file,
        string authToken,
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
                ServerEndpoint = Marshal.StringToHGlobalAuto(GetPipeName()),
                Token = Marshal.StringToHGlobalAuto(authToken),
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
            _server.WaitForPluginRegistration(authToken, cancellationToken);
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

    private string CreateAuthTokenAndSave(string pluginFile, string pluginName)
    {
        var authToken = !string.IsNullOrEmpty(ForceAuthToken) ? ForceAuthToken : Guid.NewGuid().ToString("N");
        _server.RegisterAuthToken(authToken, pluginName);
        _fileTokenMap.Add(pluginFile, authToken);
        return authToken;
    }

    private string GetPipeName() => $"{ThriftPluginClient.PluginTransportNamedPipes}://localhost/{_server.ServerEndpoint}";

    private void RegisterFunctions(IFunctionsManager functionsManager, string file,
        IEnumerable<PluginContextFunction> functions)
    {
        foreach (var function in functions)
        {
            functionsManager.RegisterFunction(function.Signature, FunctionDelegate,
                function.Description);
        }

        // This wrapper delegate is used to call external functions.
        Core.Types.VariantValue FunctionDelegate(FunctionCallInfo args)
        {
            var pluginContext = LoadPlugin(file, CancellationToken.None);
            return FunctionDelegateCall(args, pluginContext);
        }
    }

    private ThriftPluginsServer.PluginContext? GetContextByFile(string file)
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

    private string GetFileByContext(ThriftPluginsServer.PluginContext context)
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

    private void RegisterFunctions(IFunctionsManager functionsManager, ThriftPluginsServer.PluginContext pluginContext)
    {
        foreach (var function in pluginContext.Functions)
        {
            functionsManager.RegisterFunction(function.Signature, FunctionDelegate,
                function.Description);
        }

        // This wrapper delegate is used to call external functions.
        Core.Types.VariantValue FunctionDelegate(FunctionCallInfo args) => FunctionDelegateCall(args, pluginContext);
    }

    private static Core.Types.VariantValue FunctionDelegateCall(FunctionCallInfo args, ThriftPluginsServer.PluginContext pluginContext)
    {
        if (pluginContext.Client == null)
        {
            return Core.Types.VariantValue.Null;
        }

        var arguments = args.Select(SdkConvert.Convert).ToList();
        var result = AsyncUtils.RunSync(ct => pluginContext.Client.CallFunctionAsync(args.FunctionName, arguments, -1, ct));
        if (result == null)
        {
            return Core.Types.VariantValue.Null;
        }
        if (result.__isset.@object && result.Object != null)
        {
            var obj = CreateObjectFromResult(result, pluginContext);
            pluginContext.ObjectsStorage.Add(obj);
            return Core.Types.VariantValue.CreateFromObject(obj);
        }
        return SdkConvert.Convert(result);
    }

    private static object CreateObjectFromResult(VariantValue result, ThriftPluginsServer.PluginContext context)
    {
        if (result.Object == null)
        {
            throw new InvalidOperationException(Resources.Errors.NoObject);
        }
        if (context.Client == null)
        {
            throw new InvalidOperationException(Resources.Errors.NoConnection);
        }
        if (result.Object.Type == ObjectType.ROWS_INPUT || result.Object.Type == ObjectType.ROWS_ITERATOR)
        {
            return new ThriftRemoteRowsIterator(context.Client, result.Object.Handle, result.Object.Name);
        }
        if (result.Object.Type == ObjectType.JSON && !string.IsNullOrEmpty(result.Json))
        {
            var node = JsonNode.Parse(result.Json);
            if (node != null)
            {
                return node;
            }
        }
        if (result.Object.Type == ObjectType.BLOB)
        {
            return new StreamBlobData(() => new RemoteStream(result.Object.Handle, context.Client));
        }
        throw new PluginException(string.Format(Resources.Errors.CannotCreateObject, result.Object.Type));
    }

    #region Cache

    private void CacheFunctions(ThriftPluginsServer.PluginContext context, string fileName)
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
