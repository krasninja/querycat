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
using QueryCat.Plugins.Client;

namespace QueryCat.Backend.ThriftPlugins;

public sealed partial class ThriftPluginsServer : IDisposable
{
    public enum TransportType
    {
        NamedPipes,
    }

    private readonly record struct RegistrationTokenData(
        SemaphoreSlim Semaphore,
        string PluginName);

    private const int PluginRegistrationTimeoutSeconds = 10;

    public string ServerEndpoint => _mainClientConnection.Endpoint;

    private readonly IInputConfigStorage _inputConfigStorage;
    private readonly IExecutionThread _executionThread;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<string, RegistrationTokenData> _registrationTokens = new();
    private readonly List<ThriftPluginContext> _plugins = new();
    private readonly Dictionary<long, ThriftPluginContext> _tokenPluginContextMap = new();
    private readonly List<ThriftClientConnection> _clientConnections = new();
    private readonly ThriftClientConnection _mainClientConnection;
    private readonly TransportType _transportType;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsServer));

    /// <summary>
    /// Do not verify token for debug purpose.
    /// </summary>
    public bool SkipTokenVerification { get; set; }

    internal IReadOnlyCollection<ThriftPluginContext> Plugins => _plugins;

    /// <summary>
    /// Do not use application logger for Thrift internal logs.
    /// </summary>
    internal bool IgnoreThriftLogs { get; set; }

    internal sealed class PluginRegistrationEventArgs : EventArgs
    {
        public ThriftPluginContext PluginContext { get; }

        public string RegistrationToken { get; }

        public long Token { get; }

        /// <inheritdoc />
        public PluginRegistrationEventArgs(ThriftPluginContext pluginContext, string registrationToken, long token)
        {
            PluginContext = pluginContext;
            RegistrationToken = registrationToken;
            Token = token;
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
        _transportType = transportType;
        _inputConfigStorage = executionThread.ConfigStorage;
        if (string.IsNullOrEmpty(serverEndpoint))
        {
            serverEndpoint = GenerateEndpointAddress(_transportType);
        }
        var server = CreateServer(transportType, serverEndpoint);
        _mainClientConnection = new ThriftClientConnection(serverEndpoint, server);
    }

    private TThreadPoolAsyncServer CreateServer(TransportType transportType, string endpoint)
    {
        var transport = CreateTransport(transportType, endpoint);
        var transportFactory = new TFramedTransport.Factory();
        var binaryProtocolFactory = new TBinaryProtocol.Factory();
        var processor = new TMultiplexedProcessor();
        var handler = new HandlerWithExceptionIntercept(new Handler(this));
        var asyncProcessor = new Plugins.Sdk.PluginsManager.AsyncProcessor(handler);
        processor.RegisterProcessor(QueryCat.Plugins.Client.ThriftPluginClient.PluginsManagerServiceName, asyncProcessor);
        return new TThreadPoolAsyncServer(
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
    public void Start() => _mainClientConnection.Start(_serverCts);

    /// <summary>
    /// Stop server.
    /// </summary>
    public void Stop() => _mainClientConnection.Stop();

    private void RegisterPluginContext(ThriftPluginContext context, string registrationToken, long token)
    {
        _plugins.Add(context);
        _tokenPluginContextMap[token] = context;
        OnPluginRegistration?.Invoke(this, new PluginRegistrationEventArgs(context, registrationToken, token));
    }

    #region Token/Authorization

    internal ThriftPluginContext GetPluginContextByToken(long token)
    {
        if (!_tokenPluginContextMap.TryGetValue(token, out var context))
        {
            throw new AuthorizationException();
        }
        return context;
    }

    internal bool VerifyToken(long token) => token != -1 && _tokenPluginContextMap.ContainsKey(token);

    /// <summary>
    /// Generate new authorization token.
    /// </summary>
    /// <returns>New token.</returns>
    public long GenerateToken() => Random.Shared.NextInt64();

    #endregion

    #region Registration/Authentication

    public void SetRegistrationToken(string token, string pluginName)
    {
        _registrationTokens[token] = new RegistrationTokenData(new SemaphoreSlim(0, 1), pluginName);
    }

    public bool VerifyRegistrationToken(string token) => _registrationTokens.ContainsKey(token);

    internal string DumpRegistrationTokens()
    {
        var sb = new StringBuilder();
        foreach (var registrationToken in _registrationTokens)
        {
            sb.AppendFormat($"{registrationToken.Key}: {registrationToken.Value}");
        }
        return sb.ToString();
    }

    public string GetPluginNameByRegistrationToken(string token)
    {
        if (_registrationTokens.TryGetValue(token, out var data))
        {
            return data.PluginName;
        }
        return string.Empty;
    }

    public void ConfirmRegistrationToken(string token)
    {
        if (_registrationTokens.TryGetValue(token, out var data))
        {
            data.Semaphore.Release();
        }
    }

    public void RemoveRegistrationToken(string token)
    {
        if (_registrationTokens.TryRemove(token, out var data))
        {
            data.Semaphore.Dispose();
        }
    }

    public void WaitForPluginRegistration(string registrationToken, CancellationToken cancellationToken = default)
    {
        if (!_registrationTokens.TryGetValue(registrationToken, out var registrationTokenData))
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.TokenNotRegistered, registrationToken));
        }
        _logger.LogTrace("Waiting for token activation '{Token}'.", registrationToken);
        if (!registrationTokenData.Semaphore.Wait(TimeSpan.FromSeconds(PluginRegistrationTimeoutSeconds), cancellationToken))
        {
            throw new PluginException(string.Format(Resources.Errors.TokenRegistrationTimeout, registrationToken));
        }
        _registrationTokens.Remove(registrationToken, out _);
        registrationTokenData.Semaphore.Dispose();
    }

    #endregion

    private static string GenerateEndpointAddress(TransportType transportType) => transportType switch
    {
        TransportType.NamedPipes => "qcat-" + Guid.NewGuid().ToString("N"),
        _ => throw new ArgumentOutOfRangeException(nameof(transportType)),
    };

    internal ThriftClientConnection CreateClientConnection(ThriftPluginContext pluginContext)
    {
        var endpoint = GenerateEndpointAddress(_transportType);
        var server = CreateServer(_transportType, endpoint);
        var connection = new ThriftClientConnection(endpoint, server);
        connection.PluginContext = pluginContext;
        _clientConnections.Add(connection);
        return connection;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _serverCts.Dispose();
        foreach (var pluginContext in _plugins)
        {
            pluginContext.Dispose();
        }
        foreach (var clientConnection in _clientConnections)
        {
            clientConnection.Stop();
        }
        _clientConnections.Clear();
    }

    #region ThriftClientConnection

    internal sealed class ThriftClientConnection
    {
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftClientConnection));

        public string Endpoint { get; }

        public TServer Server { get; }

        public Task? ServerListenThread { get; private set; }

        public ThriftPluginContext? PluginContext { get; set; }

        public ThriftClientConnection(string endpoint, TServer server)
        {
            Endpoint = endpoint;
            Server = server;
        }

        /// <summary>
        /// Start local host server.
        /// </summary>
        public void Start(CancellationTokenSource cts)
        {
            if (ServerListenThread != null)
            {
                return;
            }

            Server.Start();
            ServerListenThread = Task.Factory.StartNew(() => Server.ServeAsync(cts.Token),
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
            _logger.LogDebug("Started named pipe server on '{Uri}'.", Endpoint);
        }

        /// <summary>
        /// Stop server.
        /// </summary>
        public void Stop()
        {
            if (ServerListenThread == null)
            {
                return;
            }

            try
            {
                Server.Stop();
            }
            catch (ArgumentOutOfRangeException)
            {
                // Sometimes we get this exception if server is not fully initialized yet, not sure how to handle this.
            }
            _logger.LogTrace("Plugin server stopped.");
            ServerListenThread = null;
        }
    }

    #endregion
}
