using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;

namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly IPluginLoadStrategy _pluginLoadStrategy;
    private readonly string _pluginName;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginAssemblyLoadContext));

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
        // 1. Try to use standard approach first.
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

        // 2. Try to load from "runtimes" folder.

        // Format native library name.
        var targetDllName = GetTargetDllName(unmanagedDllName);
        var files = _pluginLoadStrategy.GetAllFiles()
            .ToArray();

        // Try to load from runtime path. Example: runtimes/linux-arm64/native/libduckdb.so .
        var runtimePath = Path.Combine("runtimes", Application.GetRuntimeIdentifier(), "native", targetDllName);
        var libraryPath = FindTargetIgnorePathSeparator(runtimePath, files);
        var libraryHandle = LoadLibrary(libraryPath);
        if (libraryHandle != IntPtr.Zero)
        {
            _loaded[unmanagedDllName] = libraryHandle;
            return libraryHandle;
        }

        // Try to load from the root path.
        libraryPath = FindTargetIgnorePathSeparator(targetDllName, files);
        libraryHandle = LoadLibrary(libraryPath);
        if (libraryHandle != IntPtr.Zero)
        {
            _loaded[unmanagedDllName] = libraryHandle;
            return libraryHandle;
        }

        // 3. Try to load with NativeLibrary.
        if (NativeLibrary.TryLoad(unmanagedDllName, out address))
        {
            return address;
        }

        _logger.LogWarning("Failed to load library '{LibraryPath}'.", libraryPath);
        return IntPtr.Zero;
    }

    private static string FindTargetIgnorePathSeparator(string target, string[] files)
    {
        foreach (var file in files)
        {
            if (file.EndsWith(target))
            {
                return file;
            }
            // Attempt to find using alternative separator.
            if (file.IndexOf(Path.DirectorySeparatorChar) < 0)
            {
                if (file
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    .EndsWith(target))
                {
                    return file;
                }
            }
        }

        return target.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
            libraryPath = CopyFileToNativeCache(libraryPath);
        }

        // Load native library.
        if (NativeLibrary.TryLoad(libraryPath, out var library))
        {
            _logger.LogDebug("Loaded native library '{LibraryPath}'.", libraryPath);
            return library;
        }

        return IntPtr.Zero;
    }

    private string CopyFileToNativeCache(string libraryPath)
    {
        var libraryName = Path.GetFileName(libraryPath);
        using var file = _pluginLoadStrategy.GetFile(libraryPath);
        if (file == Stream.Null)
        {
            return string.Empty;
        }
        var cacheTargetDirectory = Path.Combine(Application.GetApplicationDirectory(), "native-cache", _pluginName);
        Directory.CreateDirectory(cacheTargetDirectory);
        libraryPath = Path.Combine(cacheTargetDirectory, libraryName);
        long fileSize = 0;
        if (file.CanSeek)
        {
            file.Seek(0, SeekOrigin.Begin);
            fileSize = file.Length;
        }
        else
        {
            fileSize = _pluginLoadStrategy.GetFileSize(libraryPath);
        }
        if (!File.Exists(libraryPath) ||
            new FileInfo(libraryPath).Length != fileSize)
        {
            using var newFile = File.Create(libraryPath);
            file.CopyTo(newFile);
            newFile.Close();
            _logger.LogDebug("Cached native library '{FilePath}'.", libraryPath);
        }
        return libraryPath;
    }

    private static string GetTargetDllName(string unmanagedDllName)
    {
        var prefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? string.Empty : "lib";

        var fileExtension = Path.GetExtension(unmanagedDllName);
        var targetExtension = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && !fileExtension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            targetExtension = ".dll";
        }
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            && !fileExtension.Equals(".so"))
        {
            targetExtension = ".so";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            && !fileExtension.Equals(".dylib"))
        {
            targetExtension = ".dylib";
        }

        return $"{prefix}{unmanagedDllName}{targetExtension}";
    }
}
