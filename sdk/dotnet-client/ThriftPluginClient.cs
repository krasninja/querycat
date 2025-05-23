using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Client.Logging;
using QueryCat.Plugins.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient : IDisposable
{
    public const string PluginsManagerServiceName = "plugins-manager";
    public const string PluginServerName = "plugin";
    public const string TestRegistrationToken = "test";
    public const string TestPipeName = "qcat-test";

    public const string PluginServerPipeParameter = "server-endpoint";
    public const string PluginTokenParameter = "token";
    public const string PluginParentPidParameter = "parent-pid";
    public const string PluginLogLevelParameter = "log-level";
    public const string PluginDebugServerParameter = "debug-server";
    public const string PluginDebugServerQueryParameter = "debug-server-query";
    public const string PluginDebugServerFileParameter = "debug-server-file";
    public const string PluginDebugServerFollowParameter = "debug-server-follow";

    public const string PluginMainFunctionName = "QueryCatPlugin_Main";

    private readonly ThriftPluginExecutionThread _executionThread;
    private readonly PluginFunctionsManager _functionsManager;
    private readonly ObjectsStorage _objectsStorage = new();
    private readonly string _debugServerPath = string.Empty;
    private readonly string _debugServerQueryText = string.Empty;
    private readonly string _debugServerQueryFile = string.Empty;
    private readonly bool _debugServerFollow;
    private readonly int _parentPid;
    private Process? _qcatProcess;
    private readonly SemaphoreSlim _exitSemaphore = new(0, 1);
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginClient));

    // Connection to plugin manager.
    private readonly Uri _pluginServerUri;
    private readonly string _registrationToken;
    private readonly TProtocol _protocol;
    private readonly PluginsManager.Client _thriftClient;
    private readonly ThreadSafePluginsManagerClient _thriftClientSafe;

    // Plugin server.
    private readonly List<ThriftServerConnection> _serverConnections = new();
    private readonly object _objLock = new();
    private readonly CancellationTokenSource _clientServerCts = new();

    /// <summary>
    /// Functions manager.
    /// </summary>
    public IFunctionsManager FunctionsManager => _functionsManager;

    /// <summary>
    /// Registration result. It is filled after connection to QueryCat host.
    /// </summary>
    public RegistrationResult? RegistrationResult { get; private set; }

    /// <summary>
    /// Do not use application logger for Thrift internal logs.
    /// </summary>
    internal bool IgnoreThriftLogs { get; set; }

    /// <summary>
    /// Is current client connected and registered.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Plugin token.
    /// </summary>
    public long Token => RegistrationResult != null ? RegistrationResult.Token : -1;

    internal PluginsManager.IAsync ThriftClient => _thriftClientSafe;

    /// <summary>
    /// The event occurs on plugin registration.
    /// </summary>
    public EventHandler<ThriftPluginClientOnInitializeEventArgs>? OnInitialize;

    public ThriftPluginClient(ThriftPluginClientArguments args)
    {
#if !DEBUG
        IgnoreThriftLogs = true;
#endif

        // Server pipe.
        var serverEndpoint = !string.IsNullOrEmpty(args.DebugServerPath)
            ? ThriftTransportUtils.FormatTransportUri(ThriftTransportType.NamedPipes, TestPipeName)
            : new Uri(args.ServerEndpoint);
        _pluginServerUri = serverEndpoint;

        // Auth token.
        if (string.IsNullOrEmpty(args.DebugServerPath))
        {
            _registrationToken = args.RegistrationToken;
        }
        else
        {
            _registrationToken = TestRegistrationToken;
        }

        // Parent PID.
        _parentPid = args.ParentPid;

        // Debug server.
        if (!string.IsNullOrEmpty(args.DebugServerPath))
        {
            _debugServerPath = args.DebugServerPath;
            _debugServerQueryText = args.DebugServerQueryText;
            _debugServerQueryFile = args.DebugServerQueryFile;
            _debugServerFollow = args.DebugServerFollow;
        }

        // Bootstrap.
        _protocol = new TMultiplexedProtocol(
            new TBinaryProtocol(
                new TFramedTransport(
                    ThriftTransportUtils.CreateClientTransport(serverEndpoint))
                ),
            PluginsManagerServiceName);
        _thriftClient = new PluginsManager.Client(_protocol);
        _thriftClientSafe = new ThreadSafePluginsManagerClient(_thriftClient);

        _executionThread = new ThriftPluginExecutionThread(this);
        _functionsManager = new PluginFunctionsManager();
    }

    public static ThriftPluginClientArguments ConvertCommandLineArguments(string[] args)
    {
        // The special case for testing.
        if (args.Length > 0 && args[0] == "bypass")
        {
            return new ThriftPluginClientArguments();
        }
        if (args.Length == 0)
        {
            throw new InvalidOperationException(Resources.Errors.MustBeRunByHost);
        }

        var appArgs = new ThriftPluginClientArguments();
        foreach (var arg in args)
        {
            var separatorIndex = arg.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex == -1)
            {
                throw new InvalidOperationException(string.Format(Resources.Errors.InvalidArgument, arg));
            }
            var name = arg.Substring(2, separatorIndex - 2);
            var value = arg.Substring(separatorIndex + 1);
            switch (name)
            {
                case PluginServerPipeParameter:
                // Legacy.
                case "server-pipe-name":
                    appArgs.ServerEndpoint = value;
                    break;
                case PluginTokenParameter:
                    appArgs.RegistrationToken = value;
                    break;
                case PluginParentPidParameter:
                    appArgs.ParentPid = int.Parse(value);
                    break;
                case PluginLogLevelParameter:
                    appArgs.LogLevel = Enum.Parse<LogLevel>(value, ignoreCase: true);
                    break;
                case PluginDebugServerParameter:
                    appArgs.DebugServerPath = value;
                    break;
                case PluginDebugServerQueryParameter:
                    appArgs.DebugServerQueryText = value;
                    break;
                case PluginDebugServerFileParameter:
                    appArgs.DebugServerQueryFile = value;
                    break;
                case PluginDebugServerFollowParameter:
                    appArgs.DebugServerFollow = bool.Parse(value);
                    break;
                default:
                    continue;
            }
        }

        return appArgs;
    }

    public static void SetupApplicationLogging(LogLevel? logLevel = null)
    {
        var minLogLevel = LogLevel.Information;
#if DEBUG
        minLogLevel = LogLevel.Debug;
#endif
        if (logLevel.HasValue)
        {
            minLogLevel = logLevel.Value;
        }

        Application.LoggerFactory = new LoggerFactory(
            providers: [
                new SimpleConsoleLoggerProvider(LogLevel.Error),
            ],
            new LoggerFilterOptions
            {
                MinLevel = minLogLevel,
            });

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
        Environment.Exit(1);
    }

    /// <summary>
    /// Start client server and register the plugin.
    /// </summary>
    /// <param name="pluginData">Plugin information. If null - the info will be retrieved from current assembly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StartAsync(PluginData? pluginData = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_debugServerPath))
        {
            StartQueryCatDebugServer();
            Thread.Sleep(100);
        }

        var task = _thriftClient.OpenTransportAsync(cancellationToken);
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new PluginException(string.Format(Resources.Errors.CannotConnectPluginManager, _pluginServerUri));
        }

        var callbackUri = StartNewServer();

        var functions = _functionsManager.GetPluginFunctions()
            .OfType<PluginFunction>()
            .Select(f =>
            new Function(f.Signature, f.Description, false)
            {
                IsSafe = f.IsSafe,
                FormatterIds = f.Formatters.ToList(),
            }).ToList();
        pluginData ??= SdkConvert.Convert(Assembly.GetEntryAssembly());
        pluginData.Functions = functions;

        RegistrationResult = await _thriftClient.RegisterPluginAsync(
            _registrationToken,
            callbackUri.ToString(),
            pluginData,
            cancellationToken);
        IsActive = true;

        // Add the current logger.
        QueryCat.Backend.Core.Application.LoggerFactory.AddProvider(new ThriftClientLoggerProvider(this));
    }

    internal Uri StartNewServer(ThriftTransportType transportType = ThriftTransportType.NamedPipes)
    {
        var id = ThriftTransportUtils.GenerateIdentifier();
        var uri = ThriftTransportUtils.FormatTransportUri(transportType, $"qcatp-{id}");

        var transport = ThriftTransportUtils.CreateServerTransport(uri);
        var transportFactory = new TFramedTransport.Factory();
        var binaryProtocolFactory = new TBinaryProtocol.Factory();
        var processor = new TMultiplexedProcessor();
        var handler = new HandlerWithExceptionIntercept(new Handler(this));
        var asyncProcessor = new Plugin.AsyncProcessor(handler);
        processor.RegisterProcessor(PluginServerName, asyncProcessor);
        var clientServer = new TThreadPoolAsyncServer(
            new TSingletonProcessorFactory(processor),
            transport,
            transportFactory,
            transportFactory,
            binaryProtocolFactory,
            binaryProtocolFactory,
            default,
            IgnoreThriftLogs
                ? NullLoggerFactory.Instance.CreateLogger(nameof(TSimpleAsyncServer))
                : Application.LoggerFactory.CreateLogger(nameof(TSimpleAsyncServer)));
        var serverConnection = new ThriftServerConnection(clientServer);

        lock (_objLock)
        {
            _serverConnections.Add(serverConnection);
            serverConnection.Start();
        }

        return uri;
    }

    private void StopServer()
    {
        IsActive = false;

        // Connection to plugin manager.
        _protocol.Dispose();
        _thriftClient.Dispose();

        // Server.
        _clientServerCts.Dispose();
        lock (_objLock)
        {
            foreach (var connection in _serverConnections)
            {
                connection.Stop();
            }
        }

        _exitSemaphore.Release();
        _exitSemaphore.Dispose();
    }

    private void StartQueryCatDebugServer()
    {
        if (_qcatProcess != null)
        {
            return;
        }

        var modulePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(modulePath))
        {
            throw new InvalidOperationException(Resources.Errors.CannotGetPath);
        }

        _qcatProcess = new Process
        {
            StartInfo =
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = _debugServerPath,
            }
        };
        _qcatProcess.StartInfo.Arguments = "plugin debug";
        if (!string.IsNullOrEmpty(_debugServerQueryText))
        {
            _qcatProcess.StartInfo.Arguments += " \"" + _debugServerQueryText.Replace("\"", "\\\"") + "\" ";
        }
        else if (!string.IsNullOrEmpty(_debugServerQueryFile))
        {
            _qcatProcess.StartInfo.Arguments += " -f \"" + _debugServerQueryFile + "\" ";
        }
        if (_debugServerFollow)
        {
            _qcatProcess.StartInfo.Arguments += " --follow ";
        }
        _qcatProcess.StartInfo.Arguments += $"--log-level=trace --plugin-dirs=\"{modulePath}\"";
        _logger.LogDebug("qcat host arguments '{Arguments}'.", _qcatProcess.StartInfo.Arguments);
        _qcatProcess.OutputDataReceived += (_, args) => Console.Out.WriteLine($"> {args.Data}");
        _qcatProcess.ErrorDataReceived += (_, args) => Console.Error.WriteLine($"> {args.Data}");
        _qcatProcess.Start();
        _qcatProcess.BeginOutputReadLine();
        _qcatProcess.BeginErrorReadLine();
        _logger.LogDebug("Start qcat host.");
    }

    /// <summary>
    /// Wait for server (parent QCat process or thread) exit.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForServerExitAsync(CancellationToken cancellationToken = default)
    {
        var waitingTasks = new List<Task>();
        if (_qcatProcess != null)
        {
            waitingTasks.Add(_qcatProcess.WaitForExitAsync(cancellationToken));
        }
        if (_parentPid > 0)
        {
            var process = Process.GetProcessById(_parentPid);
            waitingTasks.Add(process.WaitForExitAsync(cancellationToken));
        }
        waitingTasks.Add(_exitSemaphore.WaitAsync(cancellationToken));
        await Task.WhenAny(waitingTasks);
    }

    /// <summary>
    /// Signal that plugin can be terminated.
    /// </summary>
    public void SignalExit()
    {
        _exitSemaphore.Release();
    }

    /// <summary>
    /// Log thru QueryCat host.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <param name="message">Log message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="args">Log arguments.</param>
    public async Task LogAsync(global::QueryCat.Plugins.Sdk.LogLevel level, string message, CancellationToken cancellationToken, params string[] args)
    {
        await ThriftClient.LogAsync(Token, level, message, args.ToList(), cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopServer();

            _executionThread.Dispose();
            _qcatProcess?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal sealed class ThriftServerConnection
    {
        public TServer ClientServer { get; }

        public Task? ClientServerListenThread { get; private set; }

        public ThriftServerConnection(TServer clientServer)
        {
            ClientServer = clientServer;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            ClientServer.Start();
            ClientServerListenThread = Task.Factory.StartNew(
                () => ClientServer.ServeAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
        }

        public void Stop()
        {
            ClientServer.Stop();
        }
    }
}
