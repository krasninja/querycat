using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Server;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.ThriftPlugins;

public sealed partial class ThriftPluginsServer : IDisposable
{
    public enum TransportType
    {
        NamedPipes,
    }

    private readonly record struct AuthTokenData(
        SemaphoreSlim Semaphore,
        string PluginName);

    private const int PluginRegistrationTimeoutSeconds = 10;

    private readonly IInputConfigStorage _inputConfigStorage;
    private readonly IExecutionThread _executionThread;
    private readonly TServer _server;
    private Task? _serverListenThread;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<string, AuthTokenData> _authTokens = new();
    private readonly List<PluginContext> _plugins = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsServer));

    public string ServerEndpoint { get; } = "qcat-" + Guid.NewGuid().ToString("N");

    /// <summary>
    /// Do not verify token for debug purpose.
    /// </summary>
    public bool SkipTokenVerification { get; set; }

    internal IReadOnlyCollection<PluginContext> Plugins => _plugins;

    /// <summary>
    /// Do not use application logger for Thrift internal logs.
    /// </summary>
    internal bool IgnoreThriftLogs { get; set; }

    internal sealed class PluginRegistrationEventArgs : EventArgs
    {
        public PluginContext PluginContext { get; }

        public string AuthToken { get; }

        /// <inheritdoc />
        public PluginRegistrationEventArgs(PluginContext pluginContext, string authToken)
        {
            PluginContext = pluginContext;
            AuthToken = authToken;
        }
    }

    internal event EventHandler<PluginRegistrationEventArgs>? OnPluginRegistration;

    public ThriftPluginsServer(
        IExecutionThread executionThread,
        TransportType transportType = TransportType.NamedPipes,
        string? serverEndpoint = null)
    {
#if !DEBUG
        IgnoreThriftLogs = true;
#endif
        _executionThread = executionThread;
        _inputConfigStorage = executionThread.ConfigStorage;
        if (!string.IsNullOrEmpty(serverEndpoint))
        {
            ServerEndpoint = serverEndpoint;
        }

        var transport = CreateTransport(transportType, ServerEndpoint);
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
                ? NullLoggerFactory.Instance.CreateLogger(nameof(TThreadPoolAsyncServer))
                : Application.LoggerFactory.CreateLogger(nameof(TThreadPoolAsyncServer)));
    }

    private static TServerTransport CreateTransport(TransportType transportType, string endpoint)
    {
        switch (transportType)
        {
            case TransportType.NamedPipes:
                return new TNamedPipeServerTransport(endpoint, new TConfiguration(),
                    NamedPipeServerFlags.OnlyLocalClients, 1);
        }
        throw new ArgumentOutOfRangeException(nameof(transportType));
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
        _logger.LogDebug("Started named pipe server on '{Uri}'.", ServerEndpoint);
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

    private void RegisterPluginContext(PluginContext context, string authToken)
    {
        _plugins.Add(context);
        OnPluginRegistration?.Invoke(this, new PluginRegistrationEventArgs(context, authToken));
    }

    public void RegisterAuthToken(string token, string pluginName)
    {
        _authTokens[token] = new AuthTokenData(new SemaphoreSlim(0, 1), pluginName);
    }

    public bool VerifyAuthToken(string token) => _authTokens.ContainsKey(token);

    internal string DumpAuthTokens()
    {
        var sb = new StringBuilder();
        foreach (var authToken in _authTokens)
        {
            sb.AppendFormat($"{authToken.Key}: {authToken.Value}");
        }
        return sb.ToString();
    }

    public string GetPluginNameByAuthToken(string token)
    {
        if (_authTokens.TryGetValue(token, out var data))
        {
            return data.PluginName;
        }
        return string.Empty;
    }

    public void ConfirmAuthToken(string token)
    {
        if (_authTokens.TryGetValue(token, out var data))
        {
            data.Semaphore.Release();
        }
    }

    public void RemoveAuthToken(string token)
    {
        if (_authTokens.TryRemove(token, out var data))
        {
            data.Semaphore.Dispose();
        }
    }

    public void WaitForPluginRegistration(string authToken, CancellationToken cancellationToken = default)
    {
        if (!_authTokens.TryGetValue(authToken, out var authTokenData))
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.TokenNotRegistered, authToken));
        }
        _logger.LogTrace("Waiting for token activation '{Token}'.", authToken);
        if (!authTokenData.Semaphore.Wait(TimeSpan.FromSeconds(PluginRegistrationTimeoutSeconds), cancellationToken))
        {
            throw new PluginException(string.Format(Resources.Errors.TokenRegistrationTimeout, authToken));
        }
        _authTokens.Remove(authToken, out _);
        authTokenData.Semaphore.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _serverCts.Dispose();
        foreach (var pluginContext in _plugins)
        {
            pluginContext.Shutdown();
        }
    }
}
