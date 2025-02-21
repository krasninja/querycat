using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Thrift.Protocol;
using Thrift.Transport;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

[DebuggerDisplay("Name = {PluginName}, Connections = {CurrentConnectionsCount}")]
internal sealed class ThriftPluginContext : IDisposable, IAsyncDisposable
{
    private const int MaxConnections = 1;

    private readonly ConcurrentQueue<string> _pluginCallbackUris = new();
    private static readonly SimpleObjectPool<WaitingConsumer> _waitingConsumerPool = new(() => new WaitingConsumer());
    private readonly ConcurrentQueue<WaitingConsumer> _awaitClientQueue = new();
    private readonly ConcurrentQueue<ClientWrapper> _clientWrappers = new();
    private readonly SemaphoreSlim _createClientSemaphore = new(1);
    private int _currentConnectionsCount;
    private bool _maxConnectionsReached;
    private bool _isDisposed;
    private readonly bool _logClientRemoteCalls;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginContext));

    public int CurrentConnectionsCount => _currentConnectionsCount;

    public string PluginName { get; set; } = "N/A";

    public List<PluginContextFunction> Functions { get; } = new();

    public ObjectsStorage ObjectsStorage { get; } = new();

    public IntPtr? LibraryHandle { get; set; }

    public ThriftPluginContext(string firstCallbackUri, bool logClientRemoteCalls = false)
    {
        _pluginCallbackUris.Enqueue(firstCallbackUri);
        _logClientRemoteCalls = logClientRemoteCalls;
    }

    internal async ValueTask<ClientSession> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ThriftPluginContext));
        }

        if (_currentConnectionsCount == 0)
        {
            await CreateClientAsync(null, cancellationToken);
        }

        // Has available client - use it.
        if (_clientWrappers.TryDequeue(out var clientWrapper))
        {
            _logger.LogTrace("Take connection {ConnectionId}.", clientWrapper.Id);
            return new ClientSession(this, clientWrapper);
        }

        // Create the new awaiter and wait.
        var awaiter = _waitingConsumerPool.Get();
        ClientSession clientSession;
        try
        {
            _awaitClientQueue.Enqueue(awaiter);
            await awaiter.Trigger.WaitAsync(cancellationToken);
            if (awaiter.Session == null)
            {
                throw new InvalidOperationException("Session is not set.");
            }
            clientSession = awaiter.Session.Value;
        }
        finally
        {
            awaiter.Session = null;
            _waitingConsumerPool.Return(awaiter);
        }
        _logger.LogTrace("Take connection {ConnectionId}.", clientSession.Wrapper.Id);

        // First try to create the additional client.
        if (!_maxConnectionsReached)
        {
            await CreateClientAsync(clientSession.ClientProxy, cancellationToken);
        }

        // And then give it to the consumer.
        return clientSession;
    }

    private async Task CreateClientAsync(Plugin.IAsync? client, CancellationToken cancellationToken = default)
    {
        try
        {
            // Concurrency control.
            await _createClientSemaphore.WaitAsync(cancellationToken);

            // Do not reach connections limit.
            if (_maxConnectionsReached || _currentConnectionsCount >= MaxConnections)
            {
                _logger.LogTrace("Maximum number of connections {MaxConnections} reached.",
                    _currentConnectionsCount);
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
                ReturnAvailableClient(newClient);
            }
        }
        finally
        {
            _createClientSemaphore.Release();
        }
    }

    private async Task<ClientWrapper?> PrepareClientWrapperAsync(string callbackUri, CancellationToken cancellationToken = default)
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
            var logClient = new PluginClientLogDecorator(new Plugin.Client(protocol),
                Application.LoggerFactory.CreateLogger(nameof(PluginClientLogDecorator)));
            await logClient.OpenTransportAsync(cancellationToken);
            newClient = logClient;
        }

        // The new client will be returned to the queue after session release.
        Interlocked.Increment(ref _currentConnectionsCount);
        var wrapper = new ClientWrapper(newClient);
        _logger.LogTrace("Created new client connection {ConnectionId}, current count {ConnectionsCount}.",
            wrapper.Id,
            _currentConnectionsCount);

        return wrapper;
    }

    private void ReturnAvailableClient(ClientWrapper clientWrapper)
    {
        _logger.LogTrace("Return connect {ConnectionId}.", clientWrapper.Id);
        if (_awaitClientQueue.TryDequeue(out var consumer))
        {
            consumer.Session = new ClientSession(this, clientWrapper);
            consumer.Trigger.Release();
        }
        else
        {
            _clientWrappers.Enqueue(clientWrapper);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await _createClientSemaphore.WaitAsync();
        _createClientSemaphore.Dispose();
        ObjectsStorage.Clean();
        foreach (var clientWrapper in _clientWrappers)
        {
            await clientWrapper.Client.ShutdownAsync();
            (clientWrapper.Client as IDisposable)?.Dispose();
        }
        _clientWrappers.Clear();
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _createClientSemaphore.Dispose();
        foreach (var clientWrapper in _clientWrappers)
        {
            (clientWrapper.Client as IDisposable)?.Dispose();
        }
        if (LibraryHandle.HasValue && LibraryHandle.Value != IntPtr.Zero)
        {
            // For some reason it causes SIGSEGV (Address boundary error) on Linux.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeLibrary.Free(LibraryHandle.Value);
            }
        }
        _isDisposed = true;
    }

    private sealed class WaitingConsumer
    {
        public SemaphoreSlim Trigger { get; } = new(0, 1);

        public ClientSession? Session { get; set; }
    }

    [DebuggerDisplay("Id = {Id}")]
    internal sealed class ClientWrapper
    {
        private static int _nextId;

        public int Id { get; } = Interlocked.Increment(ref _nextId);

        public Plugin.IAsync Client { get; }

        public ClientWrapper(Plugin.IAsync client)
        {
            Client = client;
        }
    }

    internal readonly struct ClientSession(ThriftPluginContext context, ClientWrapper value) : IDisposable
    {
        public ClientWrapper Wrapper { get; } = value;

        public Plugin.IAsync ClientProxy => Wrapper.Client;

        /// <inheritdoc />
        public void Dispose()
        {
            context.ReturnAvailableClient(Wrapper);
        }
    }
}
