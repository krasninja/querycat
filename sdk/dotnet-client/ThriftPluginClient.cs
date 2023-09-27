using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;
using PluginsManager = QueryCat.Plugins.Sdk.PluginsManager;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient : IDisposable
{
    public const string PluginsManagerServiceName = "plugins-manager";
    public const string PluginServerName = "plugin";

    private readonly PluginExecutionThread _executionThread;
    private readonly PluginFunctionsManager _functionsManager;
    private readonly ObjectsStorage _objectsStorage = new();
    private string _debugServerPath = string.Empty;
    private string _debugServerPathArgs = string.Empty;
    private int _parentPid;
    private Process? _qcatProcess;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginClient));

    // Connection to plugin manager.
    private Func<TTransport> _transportFactory;
    private string _pluginServerUri = string.Empty;
    private string _authToken = string.Empty;
    private string _clientServerNamedPipe = $"qcat-{Guid.NewGuid():N}";
    private readonly TProtocol _protocol;
    private readonly PluginsManager.Client _client;

    // Plugin server.
    private TServer? _clientServer;
    private Task? _clientServerListenThread;
    private readonly CancellationTokenSource _clientServerCts = new();

    public FunctionsManager FunctionsManager => _functionsManager;

    public ThriftPluginClient(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException("The application is intended to be executed by QueryCat host application.");
        }

        foreach (var arg in args)
        {
            var separatorIndex = arg.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex == -1)
            {
                throw new InvalidOperationException($"Invalid argument '{arg}'.");
            }
            var name = arg.Substring(2, separatorIndex - 2);
            var value = arg.Substring(separatorIndex + 1);
            switch (name)
            {
                case "server-pipe-name":
                    ParseServerPipe(value);
                    break;
                case "token":
                    ParseAuthToken(value);
                    break;
                case "parent-pid":
                    ParseParentPid(value);
                    break;
                case "debug-server":
                    ParseDebugServer(value);
                    break;
                case "debug-server-args":
                    ParseDebugServerArgs(value);
                    break;
                default:
                    continue;
            }
        }

        if (_transportFactory == null)
        {
            throw new InvalidOperationException("Transport is not initialized.");
        }
        _protocol = new TMultiplexedProtocol(
            new TBinaryProtocol(
                new TFramedTransport(_transportFactory.Invoke())),
            PluginsManagerServiceName);
        _client = new PluginsManager.Client(_protocol);

        _executionThread = new PluginExecutionThread(_client);
        _functionsManager = new PluginFunctionsManager();
    }

    public static void SetupApplicationLogging()
    {
        Application.LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                });
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

    private void ParseServerPipe(string value)
    {
        var uri = new Uri(value);
        _pluginServerUri = value;

        _transportFactory = () =>
        {
            return new TNamedPipeTransport(uri.Segments[1], new TConfiguration());
        };
    }

    private void ParseAuthToken(string value)
    {
        _authToken = value;
    }

    private void ParseParentPid(string value)
    {
        _parentPid = int.Parse(value);
    }

    private void ParseDebugServer(string value)
    {
        _debugServerPath = value;
        _authToken = "test";
        ParseServerPipe("net.pipe://localhost/qcat-test");
    }

    private void ParseDebugServerArgs(string value)
    {
        _debugServerPathArgs = value;
    }

    /// <summary>
    /// Start client server and register the plugin.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_debugServerPath))
        {
            StartQueryCatDebugServer();
            Thread.Sleep(100);
        }

        var task = _client.OpenTransportAsync(cancellationToken);
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new PluginException($"Cannot connect to plugin manager with URI '{_pluginServerUri}'.");
        }

        StartServer();

        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
        var version = assemblyName?.Version;
        var functions = _functionsManager.GetPluginFunctions().Select(f =>
            new Function(f.Signature, f.Description, false));
        await _client.RegisterPluginAsync(
            _authToken,
            $"net.pipe://localhost/{_clientServerNamedPipe}",
            new PluginData
            {
                Functions = functions.ToList(),
                Name = assemblyName?.Name ?? string.Empty,
                Version = version?.ToString() ?? "0.0.0",
            },
            cancellationToken);
    }

    private void StartServer()
    {
        if (_clientServer != null)
        {
            return;
        }

        var transport = new TNamedPipeServerTransport(_clientServerNamedPipe, new TConfiguration(), NamedPipeServerFlags.OnlyLocalClients, 1);
        var transportFactory = new TFramedTransport.Factory();
        var binaryProtocolFactory = new TBinaryProtocol.Factory();
        var processor = new TMultiplexedProcessor();
        var handler = new HandlerWithExceptionIntercept(new Handler(this));
        var asyncProcessor = new Plugin.AsyncProcessor(handler);
        processor.RegisterProcessor(PluginServerName, asyncProcessor);
        _clientServer = new TThreadPoolAsyncServer(
            new TSingletonProcessorFactory(processor),
            transport,
            transportFactory,
            transportFactory,
            binaryProtocolFactory,
            binaryProtocolFactory,
            default,
            Application.LoggerFactory.CreateLogger(nameof(ThriftPluginClient)));

        _clientServer.Start();
        _clientServerListenThread = Task.Factory.StartNew(
            () => _clientServer.ServeAsync(_clientServerCts.Token),
            _clientServerCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Current);
    }

    private void StopServer()
    {
        if (_clientServer == null)
        {
            return;
        }

        // Connection to plugin manager.
        _protocol.Dispose();
        _client.Dispose();

        // Server.
        _clientServerCts.Dispose();
        _clientServer?.Stop();

        _clientServer = null;
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
            throw new InvalidOperationException("Cannot get executable path.");
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
        if (!string.IsNullOrEmpty(_debugServerPathArgs))
        {
            _qcatProcess.StartInfo.Arguments += " " + _debugServerPathArgs + " ";
        }
        _qcatProcess.StartInfo.Arguments += $"--log-level=trace --plugin-dirs=\"{modulePath}\"";
        _qcatProcess.OutputDataReceived += (_, args) => Console.Out.WriteLine($"> {args.Data}");
        _qcatProcess.ErrorDataReceived += (_, args) => Console.Error.WriteLine($"> {args.Data}");
        _qcatProcess.Start();
        _qcatProcess.BeginOutputReadLine();
        _qcatProcess.BeginErrorReadLine();
        _logger.LogDebug("Start qcat host.");
    }

    /// <summary>
    /// Wait for parent QCat process exit.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WaitForParentProcessExitAsync(CancellationToken cancellationToken = default)
    {
        if (_qcatProcess != null)
        {
            await _qcatProcess.WaitForExitAsync(cancellationToken);
        }
        if (_parentPid > 0)
        {
            var process = Process.GetProcessById(_parentPid);
            await process.WaitForExitAsync(cancellationToken);
        }
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
}
