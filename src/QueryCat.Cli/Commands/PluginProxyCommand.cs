using System.CommandLine;
using QueryCat.Backend.Execution;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
using QueryCat.Backend.ThriftPlugins;
#endif
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS && PLUGIN_THRIFT
internal class PluginProxyCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginProxyCommand() : base("install-proxy", "Install the plugins proxy.")
    {
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);

            applicationOptions.InitializeLogger();
            Console.WriteLine(Resources.Messages.PluginProxyDownload, PluginProxyDownloader.GetLinkToPluginsProxyFile());

            var downloader = new PluginProxyDownloader(ProxyFile.GetProxyFileName());
            var applicationDirectory = ExecutionThread.GetApplicationDirectory(ensureExists: true);
            var pluginsProxyLocalFile = Path.Combine(applicationDirectory,
                ProxyFile.GetProxyFileName(includeVersion: true));
            await downloader.DownloadAsync(pluginsProxyLocalFile, cancellationToken: context.GetCancellationToken());
        });
    }
}
#endif
