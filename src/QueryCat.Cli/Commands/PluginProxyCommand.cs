using System.CommandLine;
using QueryCat.Backend.Core.Utils;
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
        this.SetHandler(applicationOptions =>
        {
            applicationOptions.InitializeLogger();
            Console.WriteLine(Resources.Messages.PluginProxyDownload, PluginProxyDownloader.GetLinkToPluginsProxyFile());

            AsyncUtils.RunSync(async ct =>
            {
                var downloader = new PluginProxyDownloader(ThriftPluginsLoader.GetProxyFileName());
                var applicationDirectory = ExecutionThread.GetApplicationDirectory(ensureExists: true);
                var pluginsProxyLocalFile = Path.Combine(applicationDirectory,
                    ThriftPluginsLoader.GetProxyFileName(includeCurrentVersion: true));
                await downloader.DownloadAsync(pluginsProxyLocalFile, ct);
            });
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption));
    }
}
#endif
