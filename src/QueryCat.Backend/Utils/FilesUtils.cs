using System.Runtime.InteropServices;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Utilities to work with files.
/// </summary>
internal static class FilesUtils
{
    /// <summary>
    /// Add executable flag to file for Posix systems.
    /// </summary>
    /// <param name="file">File to make executable.</param>
    public static void MakeUnixExecutable(string file)
    {
        if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            var mode = File.GetUnixFileMode(file);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(file, mode);
        }
    }

    /// <summary>
    /// Download file and save to target. It creates the intermediate ".downloading" file.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for downloading.</param>
    /// <param name="uri">File URI.</param>
    /// <param name="targetFile">Target file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Target saved file.</returns>
    public static async Task<string> DownloadFileAsync(
        HttpClient httpClient,
        Uri uri,
        string targetFile,
        CancellationToken cancellationToken)
    {
        // If this is the directory - let's append file name to it from URI.
        if (targetFile.EndsWith(Path.PathSeparator) || Directory.Exists(targetFile))
        {
            targetFile = Path.Combine(targetFile, Path.GetFileName(uri.LocalPath));
        }

        // Make sure directory exists.
        var fileDirectory = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }

        await using var stream = await httpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
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
