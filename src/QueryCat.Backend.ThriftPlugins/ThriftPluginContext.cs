using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using Thrift.Protocol;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftPluginContext : IDisposable, IAsyncDisposable
{
    private const int MaxConnections = 1;

    private readonly Func<TProtocol> _protocolFactory;
    private readonly ConcurrentQueue<Plugin.Client> _clients = new();
    private readonly SemaphoreSlim _semaphore = new(MaxConnections);
    private bool _isDisposed;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftPluginContext));

    public string PluginName { get; set; } = "N/A";

    public List<PluginContextFunction> Functions { get; } = new();

    public ObjectsStorage ObjectsStorage { get; } = new();

    public IntPtr? LibraryHandle { get; set; }

    public bool HasConnections => _clients.Count > 0;

    public ThriftPluginContext(Func<TProtocol> protocolFactory)
    {
        _protocolFactory = protocolFactory;
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

    private async Task<Plugin.Client> CreateClientAsync(CancellationToken cancellationToken = default)
    {
        var protocol = _protocolFactory.Invoke();
        var client = new Plugin.Client(protocol);
        await client.OpenTransportAsync(cancellationToken);

        _logger.LogTrace("Created new client, current count {ConnectionsCount}.", _clients.Count);
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
            client.Dispose();
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
            client.Dispose();
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

    internal readonly struct ClientSession(ThriftPluginContext context, Plugin.Client value) : IDisposable
    {
        public Plugin.Client Value { get; } = value;

        /// <inheritdoc />
        public void Dispose()
        {
            context._semaphore.Release();
            context._clients.Enqueue(Value);
        }
    }
}
