#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.Core;
using QueryCat.Backend.ThriftPlugins;
#endif
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS && PLUGIN_THRIFT
internal class PluginProxyCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginProxyCommand() : base("install-proxy", Resources.Messages.PluginInstallProxyCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);

            applicationOptions.InitializeLogger();
            Console.WriteLine(Resources.Messages.PluginProxyDownload, PluginProxyDownloader.GetLinkToPluginsProxyFile());

            var downloader = new PluginProxyDownloader(ProxyFile.GetProxyFileName());
            var applicationDirectory = Application.GetApplicationDirectory(ensureExists: true);
            var pluginsProxyLocalFile = Path.Combine(applicationDirectory,
                ProxyFile.GetProxyFileName(includeVersion: true));
            await downloader.DownloadAsync(pluginsProxyLocalFile, cancellationToken);
        });
    }
}
#endif
