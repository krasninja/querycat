using System.Runtime.InteropServices;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// Utilities to work with files.
/// </summary>
internal static class FilesUtils
{
    private static string[] _unixExeExtensions = [".sh", ".py", ".pl", ".rb", ".run", ".elf"];

    /// <summary>
    /// Add executable flag to file for Posix systems.
    /// </summary>
    /// <param name="file">File to make executable.</param>
    public static void MakeUnixExecutable(string file)
    {
        var extension = Path.GetExtension(file);
        if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                && (string.IsNullOrEmpty(extension) || _unixExeExtensions.Contains(extension)))
        {
            var mode = File.GetUnixFileMode(file);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(file, mode);
        }
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

        var stream = await fileStreamFactory.Invoke(cancellationToken)
            .ConfigureAwait(false);
        var fullFileNameDownloading = targetFile + ".downloading";
        await using var outputFileStream = new FileStream(fullFileNameDownloading, FileMode.OpenOrCreate);
        await stream.CopyToAsync(outputFileStream, cancellationToken)
            .ConfigureAwait(false);
        stream.Close();
        outputFileStream.Close();
        var overwrite = File.Exists(targetFile);
        File.Move(fullFileNameDownloading, targetFile, overwrite);

        return targetFile;
    }
}
