using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Server;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.ThriftPlugins;

public sealed partial class ThriftPluginsServer : IDisposable
{
    private const int PluginRegistrationTimeoutSeconds = 10;

    private readonly IInputConfigStorage _inputConfigStorage;
    private readonly ExecutionThread _executionThread;
    private readonly TServer _server;
    private Task? _serverListenThread;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _authTokens = new();
    private readonly List<PluginContext> _plugins = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<ThriftPluginsServer>();

    public string ServerPipeName { get; } = "qcat-" + Guid.NewGuid().ToString("N");

    public bool SkipTokenVerification { get; set; }

    internal IReadOnlyCollection<PluginContext> Plugins => _plugins;

    internal bool IgnoreThriftLogs { get; set; } = true;

    public ThriftPluginsServer(ExecutionThread executionThread, string? serverPipeName = null)
    {
#if DEBUG
        IgnoreThriftLogs = false;
#endif
        _executionThread = executionThread;
        _inputConfigStorage = executionThread.ConfigStorage;
        if (!string.IsNullOrEmpty(serverPipeName))
        {
            ServerPipeName = serverPipeName;
        }

        var transport = new TNamedPipeServerTransport(ServerPipeName, new TConfiguration(),
            NamedPipeServerFlags.OnlyLocalClients, 1);
        var transportFactory = new TFramedTransport.Factory();
        var binaryProtocolFactory = new TBinaryProtocol.Factory();
        var processor = new TMultiplexedProcessor();
        var handler = new HandlerWithExceptionIntercept(new Handler(this));
        var asyncProcessor = new Plugins.Sdk.PluginsManager.AsyncProcessor(handler);
        processor.RegisterProcessor(QueryCat.Plugins.Client.ThriftPluginClient.PluginsManagerServiceName, asyncProcessor);
        _server = new TThreadPoolAsyncServer(
            new TSingletonProcessorFactory(processor),
            transport,
            transportFactory,
            transportFactory,
            binaryProtocolFactory,
            binaryProtocolFactory,
            default,
            IgnoreThriftLogs
                ? NullLoggerFactory.Instance.CreateLogger<TSimpleAsyncServer>()
                : Application.LoggerFactory.CreateLogger<TSimpleAsyncServer>());
    }

    /// <summary>
    /// Start local host server.
    /// </summary>
    public void Start()
    {
        if (_serverListenThread != null)
        {
            return;
        }

        _server.Start();
        _serverListenThread = Task.Factory.StartNew(() => _server.ServeAsync(_serverCts.Token),
            _serverCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Current);
        _logger.LogDebug("Started named pipe server on '{Uri}'.", ServerPipeName);
    }

    /// <summary>
    /// Stop server.
    /// </summary>
    public void Stop()
    {
        if (_serverListenThread == null)
        {
            return;
        }

        try
        {
            _server.Stop();
        }
        catch (ArgumentOutOfRangeException)
        {
            // Sometimes we get this exception if server is not fully initialized yet, not sure how to handle this.
        }
        _logger.LogTrace("Plugin server stopped.");
        _serverListenThread = null;
    }

    public void RegisterAuthToken(string token)
    {
        _authTokens[token] = new SemaphoreSlim(0, 1);
    }

    public void WaitForPluginRegistration(string authToken, CancellationToken cancellationToken = default)
    {
        if (!_authTokens.TryGetValue(authToken, out var semaphoreSlim))
        {
            throw new InvalidOperationException(
                $"Token is not registered '{authToken}', did you call {nameof(RegisterAuthToken)}?");
        }
        _logger.LogTrace("Waiting for token activation '{Token}'.", authToken);
        if (!semaphoreSlim.Wait(TimeSpan.FromSeconds(PluginRegistrationTimeoutSeconds), cancellationToken))
        {
            throw new PluginException($"Plugin '{authToken}' registration timeout.");
        }
        _authTokens.Remove(authToken, out _);
        semaphoreSlim.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _serverCts.Dispose();
        foreach (var pluginContext in _plugins)
        {
            if (pluginContext.Client == null)
            {
                continue;
            }
            AsyncUtils.RunSync(pluginContext.Client.ShutdownAsync);
        }
    }
}
