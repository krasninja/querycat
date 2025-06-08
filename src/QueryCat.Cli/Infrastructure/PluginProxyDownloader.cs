using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.PluginsManager;

namespace QueryCat.Cli.Infrastructure;

#if ENABLE_PLUGINS && PLUGIN_THRIFT
internal sealed class PluginProxyDownloader
{
    private const string RepositoryUrl = @"https://github.com/krasninja/querycat/releases/download/";
    private readonly string _proxyFileName;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginProxyDownloader));

    public PluginProxyDownloader(string proxyFileName)
    {
        this._proxyFileName = proxyFileName;
    }

    public async Task DownloadAsync(string pluginsProxyLocalFile, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        // Download.
        var pluginsProxyRemoteFile = GetLinkToPluginsProxyFile();
        _logger.LogDebug("Download proxy with URI {Uri}.", pluginsProxyRemoteFile);
        var tempPath = Path.GetTempPath();
        var archiveFile = await FilesUtils.DownloadFileAsync(
            ct => httpClient.GetStreamAsync(pluginsProxyRemoteFile, ct),
            Path.Combine(tempPath, Path.GetFileName(pluginsProxyRemoteFile.LocalPath)),
            cancellationToken);
        _logger.LogDebug("Temporary archive file {File}.", archiveFile);

        try
        {
            // Extract.
            Console.WriteLine(Resources.Messages.PluginProxyExtract);
            var stream = await ExtractFileFromArchiveAsync(_proxyFileName, archiveFile, cancellationToken);

            // Save and make executable.
            await using var writeStream = File.OpenWrite(pluginsProxyLocalFile);
            await stream.CopyToAsync(writeStream, cancellationToken);
            writeStream.Close();
            FilesUtils.MakeUnixExecutable(pluginsProxyLocalFile);
            Console.WriteLine(Resources.Messages.PluginProxySave, pluginsProxyLocalFile);
        }
        finally
        {
            // Clean up.
            File.Delete(archiveFile);
        }
    }

    private static async Task<MemoryStream> CopyToMemoryStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    private static async Task<Stream> ExtractFileFromArchiveAsync(string targetFile, string archiveFile, CancellationToken cancellationToken)
    {
        if (archiveFile.EndsWith(".tar.gz"))
        {
            await using var gzStream = File.Open(archiveFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var tarStream = new GZipStream(gzStream, CompressionMode.Decompress);
            await using var tarReader = new TarReader(tarStream);

            while (await tarReader.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry)
            {
                if (entry.EntryType is TarEntryType.RegularFile && entry.Name == targetFile
                    && entry.DataStream != null)
                {
                    return await CopyToMemoryStreamAsync(entry.DataStream, cancellationToken);
                }
            }
        }
        else if (archiveFile.EndsWith(".zip"))
        {
            using var zip = ZipFile.OpenRead(archiveFile);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == targetFile)
                {
                    await using var stream = entry.Open();
                    return await CopyToMemoryStreamAsync(stream, cancellationToken);
                }
            }
        }

        throw new NotSupportedException(string.Format(Resources.Errors.CannotExtractFromArchvie, archiveFile));
    }

    private static string GetPlatformExtension()
    {
        return Application.GetPlatform() == Application.PlatformWindows ? ".zip" : ".tar.gz";
    }

    public static Uri GetLinkToPluginsProxyFile()
    {
        // Example: https://github.com/krasninja/querycat/releases/download/v0.8.0/qcat-plugins-proxy-0.8.0-linux-x64.tar.gz.
        var sb = new StringBuilder(120);
        sb.Append(@$"{RepositoryUrl}");
        sb.Append($"v{Application.GetShortVersion()}/");
        sb.Append($"qcat-plugins-proxy-{Application.GetShortVersion()}-{Application.GetPlatform()}-{Application.GetArchitecture()}{GetPlatformExtension()}");
        return new Uri(sb.ToString());
    }
}
#endif
