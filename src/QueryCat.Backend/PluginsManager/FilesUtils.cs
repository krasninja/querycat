using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// Utilities to work with files.
/// </summary>
internal static class FilesUtils
{
    private static readonly string[] _unixExeExtensions = [".sh", ".py", ".pl", ".rb", ".run", ".elf"];

    /// <summary>
    /// Add executable flag to file for Posix systems.
    /// </summary>
    /// <param name="file">File to make executable.</param>
    public static void MakeUnixExecutable(string file)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            return;
        }

        // Skip Windows exe and .NET assemblies.
        var info = PluginInfo.CreateFromUniversalName(Path.GetFileName(file));
        if (info.Platform == Application.PlatformWindows
            || info.Platform == Application.PlatformMulti)
        {
            return;
        }

        var mode = File.GetUnixFileMode(file);
        mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        File.SetUnixFileMode(file, mode);
    }

    /// <summary>
    /// Download file and save to target. It creates the intermediate ".downloading" file.
    /// </summary>
    /// <param name="fileStreamFactory">The factory to get the file stream content.</param>
    /// <param name="targetFile">Target file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Target saved file.</returns>
    public static async Task<string> DownloadFileAsync(
        Func<CancellationToken, Task<Stream>> fileStreamFactory,
        string targetFile,
        CancellationToken cancellationToken)
    {
        // Make sure directory exists.
        var fileDirectory = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }

        await using var stream = await fileStreamFactory.Invoke(cancellationToken)
            .ConfigureAwait(false);
        var fullFileNameDownloading = targetFile + ".downloading";
        try
        {
            await using var outputFileStream = new FileStream(fullFileNameDownloading, FileMode.Create);
            await stream.CopyToAsync(outputFileStream, cancellationToken)
                .ConfigureAwait(false);
            stream.Close();
            outputFileStream.Close();

            File.Move(fullFileNameDownloading, targetFile, true);
        }
        catch (Exception)
        {
            File.Delete(fullFileNameDownloading);
            throw;
        }

        return targetFile;
    }
}
