using System.Runtime.InteropServices;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using Thrift.Protocol;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftPluginContext
{
    public string Name { get; set; } = string.Empty;

    public TProtocol? Protocol { get; set; }

    public Plugin.Client? Client { get; set; }

    public List<PluginContextFunction> Functions { get; } = new();

    public ObjectsStorage ObjectsStorage { get; } = new();

    public IntPtr? LibraryHandle { get; set; }

    internal DisposableResource<Plugin.Client> GetClient()
    {
        if (Client == null)
        {
            throw new InvalidOperationException("Cannot get available client.");
        }
        return new DisposableResource<Plugin.Client>(Client);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        ObjectsStorage.Clean();
        if (Client != null)
        {
            await Client.ShutdownAsync(cancellationToken);
        }
        if (LibraryHandle.HasValue && LibraryHandle.Value != IntPtr.Zero)
        {
            // For some reason it causes SIGSEGV (Address boundary error) on Linux.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeLibrary.Free(LibraryHandle.Value);
            }
        }
    }

    internal readonly struct DisposableResource<T> : IDisposable
    {
        public T Value { get; }

        public DisposableResource(T value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
