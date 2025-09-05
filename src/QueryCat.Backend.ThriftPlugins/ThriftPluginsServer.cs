using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Client;

namespace QueryCat.Backend.ThriftPlugins;

public sealed partial class ThriftPluginsServer : IDisposable
{
    private readonly record struct RegistrationTokenData(
        SemaphoreSlim Semaphore,
        string PluginName);

    public Uri ServerEndpointUri => _mainServerThread.Endpoint;

    private readonly IConfigStorage _configStorage;
    private readonly IExecutionThread _executionThread;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<string, RegistrationTokenData> _registrationTokens = new();
    private readonly List<ThriftPluginContext> _plugins = new();
    private readonly Dictionary<long, ThriftPluginContext> _tokenPluginContextMap = new();
    private readonly ServerThread _mainServerThread;
    private readonly int _maxConnectionsToClient;
    private readonly ObjectsStorage _objectsStorage = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsServer));

    /// <summary>
    /// Do not verify token for debug purpose.
    /// </summary>
    public bool SkipTokenVerification { get; set; }

    /// <summary>
    /// Seconds to wait before plugin/client registration. -1 for infinite.
    /// </summary>
    public int PluginRegistrationTimeoutSeconds { get; set; } = 10;

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
        ThriftEndpoint serverEndpoint,
        int maxConnectionsToClient = 1)
    {
#if !DEBUG
        IgnoreThriftLogs = true;
#endif
        _executionThread = executionThread;
        _configStorage = executionThread.ConfigStorage;
        var uri = serverEndpoint.Uri;
        var server = CreateServer(uri);
        _mainServerThread = new ServerThread(uri, server);
        _maxConnectionsToClient = maxConnectionsToClient;
    }

    private TThreadPoolAsyncServer CreateServer(Uri uri)
    {
        var transport = ThriftTransportFactory.CreateServerTransport(uri);
        var transportFactory = new TFramedTransport.Factory();
        var binaryProtocolFactory = new TBinaryProtocol.Factory();
        var processor = new TMultiplexedProcessor();
        var handler = new HandlerWithExceptionIntercept(new Handler(this, _objectsStorage));
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

    /// <summary>
    /// Start local host server. The call does not block the current thread.
    /// </summary>
    public void Start() => _mainServerThread.Start(_serverCts);

    /// <summary>
    /// Stop server.
    /// </summary>
    public void Stop() => _mainServerThread.Stop();

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
            throw new AuthorizationException(string.Format(Resources.Errors.InvalidToken, token));
        }
        return context;
    }

    internal bool IsValidToken(long token) => token != -1 && _tokenPluginContextMap.ContainsKey(token);

    internal void ValidateToken(long token)
    {
        if (!IsValidToken(token))
        {
            throw new AuthorizationException(string.Format(Resources.Errors.InvalidToken, token));
        }
    }

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

    /// <summary>
    /// Wait for the specific registration with token. The call blocks the current thread.
    /// </summary>
    /// <param name="registrationToken">Registration token to await.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void WaitForPluginRegistration(string registrationToken, CancellationToken cancellationToken = default)
    {
        if (!_registrationTokens.TryGetValue(registrationToken, out var registrationTokenData))
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.TokenNotRegistered, registrationToken));
        }
        _logger.LogTrace("Waiting for token activation '{Token}'.", registrationToken);
        var timeout = PluginRegistrationTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(PluginRegistrationTimeoutSeconds)
            : Timeout.InfiniteTimeSpan;
        if (!registrationTokenData.Semaphore.Wait(timeout, cancellationToken))
        {
            throw new PluginException(string.Format(Resources.Errors.TokenRegistrationTimeout, registrationToken));
        }
        _registrationTokens.Remove(registrationToken, out _);
        registrationTokenData.Semaphore.Dispose();
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _objectsStorage.Clean();
        _serverCts.Dispose();
        foreach (var pluginContext in _plugins)
        {
            pluginContext.Dispose();
        }
    }

    #region ThriftClientConnection

    internal sealed class ServerThread
    {
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ServerThread));

        public Uri Endpoint { get; }

        public TServer Server { get; }

        public Task? ServerListenThread { get; private set; }

        public ThriftPluginContext? PluginContext { get; set; }

        public ServerThread(Uri endpoint, TServer server)
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
            _logger.LogDebug("Started server on '{Uri}'.", Endpoint);
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
