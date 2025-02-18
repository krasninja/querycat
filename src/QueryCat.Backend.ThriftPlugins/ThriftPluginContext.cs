using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Thrift.Protocol;
using QueryCat.Backend.Core;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftPluginContext : IDisposable, IAsyncDisposable
{
    private const int MaxConnections = 1;

    private readonly Func<TProtocol> _protocolFactory;
    private readonly ThriftPluginsServer _server;
    private readonly ConcurrentQueue<Plugin.IAsync> _clients = new();
    private readonly SemaphoreSlim _semaphore = new(MaxConnections);
    private bool _isDisposed;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginContext));
    private readonly bool _logClientRemoteCalls;

    public string PluginName { get; set; } = "N/A";

    public List<PluginContextFunction> Functions { get; } = new();

    public ObjectsStorage ObjectsStorage { get; } = new();

    public IntPtr? LibraryHandle { get; set; }

    public bool HasConnections => _clients.Count > 0;

    public ThriftPluginContext(Func<TProtocol> protocolFactory, ThriftPluginsServer server)
    {
        _protocolFactory = protocolFactory;
        _server = server;
    }

    internal async ValueTask<ClientSession> GetClientAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ThriftPluginContext));
        }

        await _semaphore.WaitAsync(cancellationToken);
        if (_clients.TryDequeue(out var client))
        {
            return new ClientSession(this, client);
        }

        client = await CreateClientAsync(cancellationToken);
        return new ClientSession(this, client);
    }

    private async Task<Plugin.IAsync> CreateClientAsync(CancellationToken cancellationToken = default)
    {
        var protocol = _protocolFactory.Invoke();
        Plugin.IAsync client;

        // Prepare client.
        if (!_logClientRemoteCalls)
        {
            var pluginClient = new Plugin.Client(protocol);
            await pluginClient.OpenTransportAsync(cancellationToken);
            client = pluginClient;
        }
        else
        {
            var logClient = new PluginClientLogDecorator(new Plugin.Client(protocol),
                Application.LoggerFactory.CreateLogger(nameof(PluginClientLogDecorator)));
            await logClient.OpenTransportAsync(cancellationToken);
            client = logClient;
        }

        // The new client will be returned to the queue after session release.
        _logger.LogTrace("Created new client, current count {ConnectionsCount}.", _clients.Count + 1);
        return client;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();
        _semaphore.Dispose();
        ObjectsStorage.Clean();
        foreach (var client in _clients)
        {
            await client.ShutdownAsync();
            (client as IDisposable)?.Dispose();
        }
        _clients.Clear();
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
        foreach (var client in _clients)
        {
            (client as IDisposable)?.Dispose();
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

    internal readonly struct ClientSession(ThriftPluginContext context, Plugin.IAsync value) : IDisposable
    {
        public Plugin.IAsync Value { get; } = value;

        /// <inheritdoc />
        public void Dispose()
        {
            context._semaphore.Release();
            context._clients.Enqueue(Value);
        }
    }
}
