using System.CommandLine;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
#if ENABLE_PLUGINS
using QueryCat.Backend.ThriftPlugins;
#endif
using QueryCat.Backend.Utils;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal class PluginProxyCommand : BaseCommand
{
    private const string RepositoryUrl = @"https://github.com/krasninja/querycat/releases/download/";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PluginProxyCommand));

    /// <inheritdoc />
    public PluginProxyCommand() : base("install-proxy", "Install the plugins proxy.")
    {
        this.SetHandler(applicationOptions =>
        {
            applicationOptions.InitializeLogger();
            var pluginsProxyRemoteFile = GetLinkToPluginsProxyFile();
            Console.WriteLine(Resources.Messages.PluginProxyDownload, pluginsProxyRemoteFile);

            AsyncUtils.RunSync(async ct =>
            {
                var applicationDirectory = ExecutionThread.GetApplicationDirectory(ensureExists: true);
                var pluginsProxyLocalFile = Path.Combine(applicationDirectory, ThriftPluginsLoader.GetProxyFileName());
                using var httpClient = new HttpClient();

                // Download.
                var tempPath = Path.GetTempPath();
                var archiveFile = await FilesUtils.DownloadFileAsync(
                    httpClient, new Uri(pluginsProxyRemoteFile), tempPath, ct);
                _logger.LogDebug("Temporary archive file {File}.", archiveFile);

                try
                {
                    // Extract.
                    Console.WriteLine(Resources.Messages.PluginProxyExtract);
                    var stream = await ExtractFileFromArchiveAsync(ThriftPluginsLoader.GetProxyFileName(), archiveFile, ct);

                    // Save and make executable.
                    await using var writeStream = File.OpenWrite(pluginsProxyLocalFile);
                    await stream.CopyToAsync(writeStream);
                    writeStream.Close();
                    FilesUtils.MakeUnixExecutable(pluginsProxyLocalFile);
                    Console.WriteLine(Resources.Messages.PluginProxySave, pluginsProxyLocalFile);
                }
                finally
                {
                    // Clean up.
                    File.Delete(archiveFile);
                }
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption));
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
        else if (Path.GetExtension(archiveFile).Equals(".zip"))
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

        throw new NotSupportedException($"Cannot extract from archive '{archiveFile}'.");
    }

    private static string GetPlatformExtension()
    {
        return Application.GetPlatform() == Application.PlatformWindows ? ".zip" : ".tar.gz";
    }

    private static string GetLinkToPluginsProxyFile()
    {
        // Example: https://github.com/krasninja/querycat/releases/download/v0.6.9/qcat-0.6.9-win-x64.zip.
        var sb = new StringBuilder();
        sb.Append(@$"{RepositoryUrl}");
        sb.Append($"v{Application.GetShortVersion()}/");
        sb.Append($"qcat-plugins-proxy-{Application.GetShortVersion()}-{Application.GetPlatform()}-{Application.GetArchitecture()}{GetPlatformExtension()}");
        return sb.ToString();
    }
}
#endif
