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
    private bool _isDisposed;
    private readonly bool _logClientRemoteCalls;
    private readonly ConcurrentBag<Plugin.IAsync> _clients = new();
    private readonly Lock _functionsLock = new();
    private readonly List<PluginContextFunction> _functions = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginContext));

    public int TotalConnectionsCount => _waitQueue.Count;

    public string PluginName { get; set; } = "N/A";

    public IReadOnlyList<PluginContextFunction> Functions => _functions;

    public ObjectsStorage ObjectsStorage { get; }

    public IntPtr? LibraryHandle { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="firstCallbackUri">First callback to connect to the plugin. It is needed
    /// because we don't have other ways to ask plugin to connect.</param>
    /// <param name="objectsStorage">Object storage.</param>
    /// <param name="logClientRemoteCalls">Log client remote calls.</param>
    /// <param name="maxConnections">Max connections that can be established to the client plugin.</param>
    public ThriftPluginContext(
        string firstCallbackUri,
        ObjectsStorage objectsStorage,
        bool logClientRemoteCalls = false,
        int maxConnections = 1)
    {
        _pluginCallbackUris.Enqueue(firstCallbackUri);
        _logClientRemoteCalls = logClientRemoteCalls;
        _maxConnections = maxConnections;
        ObjectsStorage = objectsStorage;

        _waitQueue = new WaitQueue(Application.LoggerFactory);
    }

    /// <summary>
    /// Add function to context.
    /// </summary>
    /// <param name="function">Function.</param>
    public void AddFunction(PluginContextFunction function)
    {
        lock (_functionsLock)
        {
            _functions.Add(function);
        }
    }

    internal async ValueTask<ClientWrapper> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (TotalConnectionsCount == 0)
        {
            await CreateClientAsync(null, cancellationToken)
                .ConfigureAwait(false);
            if (TotalConnectionsCount == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot establish a connection to the plugin '{PluginName}'.");
            }
        }

        // Fast path.
        var hasFastItem = _waitQueue.TryDequeue(out var session);
        if (hasFastItem && session.HasValue)
        {
            return new ClientWrapper(session.Value);
        }

        session = await _waitQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
        var wrapper = new ClientWrapper(session.Value);

        if (!hasFastItem && _waitQueue.Count < _maxConnections)
        {
            await CreateClientAsync(wrapper.ClientProxy, cancellationToken)
                .ConfigureAwait(false);
        }

        return wrapper;
    }

    private async Task CreateClientAsync(Plugin.IAsync? client, CancellationToken cancellationToken = default)
    {
        // Concurrency control.
        await _createClientSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Do not reach connections limit.
            if (_waitQueue.Count >= _maxConnections)
            {
                _logger.LogTrace("Maximum number of connections {MaxConnections} reached.",
                    _maxConnections);
                return;
            }

            // Get the next URI to connect. If not available - request one from the plugin.
            if (!_pluginCallbackUris.TryDequeue(out var uri)
                && client != null)
            {
                uri = await client.ServeAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            // Open connection.
            var newClient = await PrepareClientWrapperAsync(uri, cancellationToken)
                .ConfigureAwait(false);
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
        var protocol = new TMultiplexedProtocol(
            new TBinaryProtocol(
                new TFramedTransport(
                    ThriftTransportFactory.CreateClientTransport(new SimpleUri(callbackUri)))
            ),
            ThriftPluginClient.PluginServerName);

        // Prepare client.
        var pluginClient = new Plugin.Client(protocol);

        // Open.
        try
        {
            Plugin.IAsync newClient;
            if (!_logClientRemoteCalls)
            {
                await pluginClient.OpenTransportAsync(cancellationToken).ConfigureAwait(false);
                newClient = pluginClient;
            }
            else
            {
                var logClient = new PluginClientLogDecorator(pluginClient, Application.LoggerFactory);
                await logClient.OpenTransportAsync(cancellationToken).ConfigureAwait(false);
                newClient = logClient;
            }
            _clients.Add(pluginClient);

            // The new client will be returned to the queue after session release.
            return new PluginClientIdDecorator(newClient);
        }
        catch (Exception)
        {
            pluginClient.Dispose();
            protocol.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        await _createClientSemaphore.WaitAsync(TimeSpan.FromSeconds(20))
            .ConfigureAwait(false);
        DisposeCore(true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        DisposeCore(true);
    }

    private void DisposeCore(bool disposing)
    {
        if (disposing)
        {
            while (_clients.TryTake(out var client))
            {
                (client as IDisposable)?.Dispose();
            }
            _createClientSemaphore.Dispose();
            _clients.Clear();
            _waitQueue.Dispose();
            var handle = LibraryHandle;
            LibraryHandle = null;
            if (handle.HasValue && handle.Value != IntPtr.Zero)
            {
                // For some reason it causes SIGSEGV (Address boundary error) on Linux.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    NativeLibrary.Free(handle.Value);
                }
            }

            _logger.LogTrace("Disposed.");
        }
    }

    internal sealed class ClientWrapper : IDisposable
    {
        private readonly WaitQueue.ItemWrapper _session;
        private bool _isDisposed;

        public Plugin.IAsync ClientProxy => (Plugin.IAsync)_session.Item;

        public int ProxyId => ClientProxy is PluginClientIdDecorator clientIdDecorator ? clientIdDecorator.Id : 0;

        public ClientWrapper(WaitQueue.ItemWrapper session)
        {
            _session = session;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _session.Dispose();
        }

        /// <inheritdoc />
        public override string ToString() => "ProxyId = " + ProxyId;
    }
}
