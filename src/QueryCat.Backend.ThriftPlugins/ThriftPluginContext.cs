using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Thrift.Protocol;
using Thrift.Transport;
using QueryCat.Backend.Core;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace QueryCat.Backend.ThriftPlugins;

[DebuggerDisplay("Name = {PluginName}, Connections = {TotalConnectionsCount}")]
internal sealed class ThriftPluginContext : IDisposable, IAsyncDisposable
{
    private readonly ConcurrentQueue<string> _pluginCallbackUris = new();
    private readonly SemaphoreSlim _createClientSemaphore = new(1);
    private readonly WaitQueue _waitQueue;
    private readonly int _maxConnections;
    private bool _maxConnectionsReached;
    private bool _isDisposed;
    private readonly bool _logClientRemoteCalls;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginContext));

    public int TotalConnectionsCount => _waitQueue.Count;

    public string PluginName { get; set; } = "N/A";

    public List<PluginContextFunction> Functions { get; } = new();

    public ObjectsStorage ObjectsStorage { get; } = new();

    public IntPtr? LibraryHandle { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="firstCallbackUri">First callback to connect to the plugin. It is needed
    /// because we don't have other ways to ask plugin to connect.</param>
    /// <param name="logClientRemoteCalls">Log client remote calls.</param>
    /// <param name="maxConnections">Max connections that can be established to the client plugin.</param>
    public ThriftPluginContext(string firstCallbackUri, bool logClientRemoteCalls = false,
        int maxConnections = 1)
    {
        _pluginCallbackUris.Enqueue(firstCallbackUri);
        _logClientRemoteCalls = logClientRemoteCalls;
        _maxConnections = maxConnections;

        _waitQueue = new WaitQueue(Application.LoggerFactory);
    }

    internal async ValueTask<ClientWrapper> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        if (TotalConnectionsCount == 0)
        {
            await CreateClientAsync(null, cancellationToken);
        }

        var session = await _waitQueue.DequeueAsync(cancellationToken);
        var wrapper = new ClientWrapper(session);

        if (!_maxConnectionsReached)
        {
            await CreateClientAsync(wrapper.ClientProxy, cancellationToken);
        }

        return wrapper;
    }

    private async Task CreateClientAsync(Plugin.IAsync? client, CancellationToken cancellationToken = default)
    {
        try
        {
            // Concurrency control.
            await _createClientSemaphore.WaitAsync(cancellationToken);

            // Do not reach connections limit.
            if (_maxConnectionsReached || _waitQueue.Count >= _maxConnections)
            {
                _logger.LogTrace("Maximum number of connections {MaxConnections} reached.",
                    _waitQueue.Count);
                _maxConnectionsReached = true;
                return;
            }

            // Get the next URI to connect. If not available - request one from the plugin.
            if (!_pluginCallbackUris.TryDequeue(out var uri)
                && client != null)
            {
                uri = await client.ServeAsync(cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            // Open connection.
            var newClient = await PrepareClientWrapperAsync(uri, cancellationToken);
            if (newClient != null)
            {
                _waitQueue.Enqueue(newClient);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Created new client connection {ConnectionId}, current count {ConnectionsCount}.",
                        newClient.ToString(),
                        _waitQueue.Count);
                }
            }
        }
        finally
        {
            _createClientSemaphore.Release();
        }
    }

    private async Task<Plugin.IAsync?> PrepareClientWrapperAsync(string callbackUri, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(callbackUri);
        var protocol = new TMultiplexedProtocol(
            new TBinaryProtocol(
                new TFramedTransport(
                    ThriftTransportUtils.CreateClientTransport(uri))
            ),
            ThriftPluginClient.PluginServerName);
        Plugin.IAsync newClient;

        // Prepare client.
        if (!_logClientRemoteCalls)
        {
            var pluginClient = new Plugin.Client(protocol);
            await pluginClient.OpenTransportAsync(cancellationToken);
            newClient = pluginClient;
        }
        else
        {
            var logClient = new PluginClientLogDecorator(new Plugin.Client(protocol), Application.LoggerFactory);
            await logClient.OpenTransportAsync(cancellationToken);
            newClient = logClient;
        }

        // The new client will be returned to the queue after session release.
        return new PluginClientIdDecorator(newClient);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await _createClientSemaphore.WaitAsync();
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        ObjectsStorage.Clean();
        _createClientSemaphore.Dispose();
        _waitQueue.Dispose();
        if (LibraryHandle.HasValue && LibraryHandle.Value != IntPtr.Zero)
        {
            // For some reason it causes SIGSEGV (Address boundary error) on Linux.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeLibrary.Free(LibraryHandle.Value);
            }
        }

        _logger.LogTrace("Disposed.");
        _isDisposed = true;
    }

    internal readonly struct ClientWrapper : IDisposable
    {
        private readonly WaitQueue.ItemWrapper _session;

        public Plugin.IAsync ClientProxy => (Plugin.IAsync)_session.Item;

        public int ProxyId => ClientProxy is PluginClientIdDecorator clientIdDecorator ? clientIdDecorator.Id : 0;

        public ClientWrapper(WaitQueue.ItemWrapper session)
        {
            _session = session;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _session.Dispose();
        }

        /// <inheritdoc />
        public override string ToString() => "ProxyId = " + ProxyId;
    }
}
