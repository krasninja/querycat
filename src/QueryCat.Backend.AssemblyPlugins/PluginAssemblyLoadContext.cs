using System.Runtime.InteropServices;
using System.Runtime.Loader;
using QueryCat.Backend.Core;

namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly IPluginLoadStrategy _pluginLoadStrategy;
    private readonly string _pluginName;
    private static readonly Dictionary<string, IntPtr> _loaded = new();

    public PluginAssemblyLoadContext(
        IPluginLoadStrategy pluginLoadStrategy,
        string pluginName)
    {
        _pluginLoadStrategy = pluginLoadStrategy;
        _pluginName = pluginName;
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var address = base.LoadUnmanagedDll(unmanagedDllName);
        if (address != IntPtr.Zero)
        {
            return address;
        }

        // Try to get cached.
        if (_loaded.TryGetValue(unmanagedDllName, out var ptr))
        {
            return ptr;
        }

        var targetDllName = GetTargetDllName(unmanagedDllName);
        var files = _pluginLoadStrategy.GetAllFiles().ToArray();

        // Try to load from runtime path.
        var runtimePath = Path.Combine("runtimes", Application.GetRuntimeIdentifier(), "native", targetDllName);
        var libraryPath = files.FirstOrDefault(f => f.EndsWith(runtimePath));
        var libraryHandle = LoadLibrary(libraryPath);
        if (libraryHandle != IntPtr.Zero)
        {
            _loaded[unmanagedDllName] = libraryHandle;
            return libraryHandle;
        }

        // Try to load from the root path.
        libraryPath = files.FirstOrDefault(f => f.EndsWith(targetDllName));
        libraryHandle = LoadLibrary(libraryPath);
        if (libraryHandle != IntPtr.Zero)
        {
            _loaded[unmanagedDllName] = libraryHandle;
            return libraryHandle;
        }

        return IntPtr.Zero;
    }

    private IntPtr LoadLibrary(string? libraryPath)
    {
        if (string.IsNullOrEmpty(libraryPath))
        {
            return IntPtr.Zero;
        }

        // If file doesn't exist, probably it is not a file system.
        // We should get it from the strategy and save into QueryCat local folder.
        if (!File.Exists(libraryPath))
        {
            var libraryName = Path.GetFileName(libraryPath);
            var cacheTargetDirectory = Path.Combine(Application.GetApplicationDirectory(), "native-cache", _pluginName);
            Directory.CreateDirectory(cacheTargetDirectory);
            using var file = _pluginLoadStrategy.GetFile(libraryPath);
            if (file.Length == 0)
            {
                return IntPtr.Zero;
            }
            libraryPath = Path.Combine(cacheTargetDirectory, libraryName);
            file.Seek(0, SeekOrigin.Begin);
            if (!File.Exists(libraryPath) ||
                new FileInfo(libraryPath).Length != file.Length)
            {
                using var newFile = File.Create(libraryPath);
                file.CopyTo(newFile);
                newFile.Close();
            }
        }

        if (NativeLibrary.TryLoad(libraryPath, out var library))
        {
            return library;
        }
        return IntPtr.Zero;
    }

    private static string GetTargetDllName(string unmanagedDllName)
    {
        var prefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? string.Empty : "lib";

        var extension = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            extension = ".dll";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            extension = ".so";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            extension = ".dylib";
        }

        return $"{prefix}{unmanagedDllName}{extension}";
    }
}
