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

    private readonly IInputConfigStorage _inputConfigStorage;
    private readonly IExecutionThread _executionThread;
    private readonly TServer _server;
    private Task? _serverListenThread;
    private readonly CancellationTokenSource _serverCts = new();
    private readonly ConcurrentDictionary<string, RegistrationTokenData> _registrationTokens = new();
    private readonly List<ThriftPluginContext> _plugins = new();
    private readonly Dictionary<long, ThriftPluginContext> _tokenPluginContextMap = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginsServer));

    public string ServerEndpoint { get; } = "qcat-" + Guid.NewGuid().ToString("N");

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
        _inputConfigStorage = executionThread.ConfigStorage;
        if (!string.IsNullOrEmpty(serverEndpoint))
        {
            ServerEndpoint = serverEndpoint;
        }
        _server = CreateServer(transportType);
    }

    private TThreadPoolAsyncServer CreateServer(TransportType transportType)
    {
        var transport = CreateTransport(transportType, ServerEndpoint);
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

    private void RegisterPluginContext(ThriftPluginContext context, string registrationToken, long token)
    {
        _plugins.Add(context);
        _tokenPluginContextMap[token] = context;
        OnPluginRegistration?.Invoke(this, new PluginRegistrationEventArgs(context, registrationToken, token));
    }

    internal ThriftPluginContext GetPluginContextByToken(long token)
    {
        if (!_tokenPluginContextMap.TryGetValue(token, out var context))
        {
            throw new AuthorizationException();
        }
        return context;
    }

    internal bool VerifyToken(long token) => token != -1 && _tokenPluginContextMap.ContainsKey(token);

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
    /// Generate new authorization token.
    /// </summary>
    /// <returns>New token.</returns>
    public long GenerateToken() => Random.Shared.NextInt64();

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

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _serverCts.Dispose();
        foreach (var pluginContext in _plugins)
        {
            pluginContext.Dispose();
        }
    }
}
