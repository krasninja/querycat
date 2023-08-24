using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// Load plugins using Thrift protocol.
/// </summary>
public sealed class ThriftPluginsLoader : PluginsLoader, IDisposable
{
    private readonly ExecutionThread _thread;
    private readonly ThriftPluginsServer _server;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<ThriftPluginsLoader>();

    /// <summary>
    /// Skip actual plugins loading. For debug purposes.
    /// </summary>
    public bool SkipPluginsExecution { get; set; }

    /// <summary>
    /// Force use the specific authentication token.
    /// </summary>
    public string ForceAuthToken { get; set; } = string.Empty;

    /// <summary>
    /// The used server pipe name.
    /// </summary>
    public string ServerPipeName { get; } = "qcat-" + Guid.NewGuid().ToString("N");

    /// <inheritdoc />
    public ThriftPluginsLoader(
        ExecutionThread thread,
        IEnumerable<string> pluginDirectories,
        string? serverPipeName = null) : base(pluginDirectories)
    {
        _thread = thread;
        if (!string.IsNullOrEmpty(serverPipeName))
        {
            ServerPipeName = serverPipeName;
        }
        _server = new ThriftPluginsServer(thread, ServerPipeName);
    }

    /// <inheritdoc />
    public override Task LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (var pluginDirectory in PluginDirectories)
        {
            _logger.LogTrace("Search in '{Directory}'.", pluginDirectory);
            if (IsCorrectPluginFile(pluginDirectory) && IsMatchPlatform(pluginDirectory))
            {
                LoadPlugin(pluginDirectory, cancellationToken);
                continue;
            }
            if (!Directory.Exists(pluginDirectory))
            {
                _logger.LogDebug("Directory '{Directory}' not exists.", pluginDirectory);
                continue;
            }

            foreach (var pluginFile in Directory.EnumerateFiles(pluginDirectory))
            {
                if (IsCorrectPluginFile(pluginFile) && IsMatchPlatform(pluginFile))
                {
                    LoadPlugin(pluginFile, cancellationToken);
                }
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string pluginFile)
    {
        if (!File.Exists(pluginFile)
            || !pluginFile.Contains("plugin", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(pluginFile).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            && (File.GetUnixFileMode(pluginFile) & UnixFileMode.UserExecute) == 0)
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && !Path.GetExtension(pluginFile).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private bool IsMatchPlatform(string pluginFile)
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

    private void LoadPlugin(string file, CancellationToken cancellationToken)
    {
        _server.Start();

        var authToken = !string.IsNullOrEmpty(ForceAuthToken) ? ForceAuthToken : Guid.NewGuid().ToString("N");
        _server.RegisterAuthToken(authToken);
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
            process.StartInfo.ArgumentList.Add($"--server-pipe-name=net.pipe://localhost/{_server.ServerPipeName}");
            process.StartInfo.ArgumentList.Add($"--token={authToken}");
            process.StartInfo.ArgumentList.Add($"--parent-pid={Process.GetCurrentProcess().Id}");
            process.OutputDataReceived += (_, args) => _logger.LogTrace($"[{fileName}]: {args.Data}");
            process.ErrorDataReceived += (_, args) => _logger.LogError($"[{fileName}]: {args.Data}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        try
        {
            _server.WaitForPluginRegistration(authToken, cancellationToken);
        }
        catch (Exception)
        {
            process?.Close();
            throw;
        }

        RegisterFunctions(_thread.FunctionsManager);
    }

    private void RegisterFunctions(FunctionsManager functionsManager)
    {
        foreach (var plugin in _server.Plugins)
        {
            foreach (var functionSignature in plugin.Functions)
            {
                functionsManager.RegisterFunction(functionSignature, FunctionDelegate);
            }

            Core.Types.VariantValue FunctionDelegate(FunctionCallInfo args)
            {
                if (plugin.Client == null)
                {
                    return Core.Types.VariantValue.Null;
                }

                var arguments = args.Select(SdkConvert.Convert).ToList();
                var result = AsyncUtils.RunSync(() => plugin.Client.CallFunctionAsync(args.FunctionName, arguments, 0));
                if (result == null)
                {
                    return Core.Types.VariantValue.Null;
                }
                if (result.__isset.@object && result.Object != null)
                {
                    var obj = CreateObjectFromResult(result, plugin);
                    plugin.ObjectsStorage.Add(obj);
                    return Core.Types.VariantValue.CreateFromObject(obj);
                }
                return SdkConvert.Convert(result);
            }
        }
    }

    private static object CreateObjectFromResult(VariantValue result, ThriftPluginsServer.PluginContext context)
    {
        if (result.Object == null)
        {
            throw new InvalidOperationException("No object.");
        }
        if (context.Client == null)
        {
            throw new InvalidOperationException("No connection.");
        }
        if (result.Object.Type == ObjectType.ROWS_INPUT || result.Object.Type == ObjectType.ROWS_ITERATOR)
        {
            var iterator = new ThriftRemoteRowsIterator(context.Client, result.Object.Handle);
            iterator.Open();
            return iterator;
        }
        throw new PluginException("Cannot create object.");
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
