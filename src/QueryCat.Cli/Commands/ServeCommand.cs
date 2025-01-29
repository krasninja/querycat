using System.CommandLine;
using System.Net;
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

internal class ServeCommand : BaseCommand
{
    /// <inheritdoc />
    public ServeCommand() : base("serve", "Run simple HTTP server.")
    {
        var urlsOption = new Option<string>("--url", description: "Endpoint to serve on.");
        var allowOriginOption = new Option<string>("--allow-origin", description: "Enables CORS for the specified origin.");
        var passwordOption = new Option<string>("--password", description: "Basic authentication password.");
        var rootDirectoryOption = new Option<string>(aliases: ["-r", "--root-dir"], description: "Root directory for files serve.");
        var safeModeOption = new Option<bool>("--safe-mode",
            description: "Allow to call only safe (no modifications) functions.");
        var allowedIPsSlotsOption = new Option<int?>("--allowed-ips-slots",
            description: "Number of IPs that will be added to authorized list on first connect.");
        var allowedIPsOption = new Option<string[]>("--allowed-ips",
            description: "Allowed IP addresses to connect.\nExample: http://192.168.1.125:5555/")
        {
            AllowMultipleArgumentsPerToken = true,
        };

        AddOption(urlsOption);
        AddOption(allowOriginOption);
        AddOption(passwordOption);
        AddOption(rootDirectoryOption);
        AddOption(safeModeOption);
        AddOption(allowedIPsOption);
        AddOption(allowedIPsSlotsOption);
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var urls = OptionsUtils.GetValueForOption(urlsOption, context);
            var allowOrigin = OptionsUtils.GetValueForOption(allowOriginOption, context);
            var password = OptionsUtils.GetValueForOption(passwordOption, context);
            var safeMode = OptionsUtils.GetValueForOption(safeModeOption, context);
            var rootDirectory = OptionsUtils.GetValueForOption(rootDirectoryOption, context);
            var allowedIPs = OptionsUtils.GetValueForOption(allowedIPsOption, context);
            var allowedIPsSlots = OptionsUtils.GetValueForOption(allowedIPsSlotsOption, context);

            applicationOptions.InitializeLogger();
            using var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.Options.AddRowNumberColumn = true;
            root.Thread.Options.SafeMode = safeMode;
            var webServer = new WebServer(root.Thread, new WebServerOptions
            {
                Urls = urls,
                Password = password,
                FilesRoot = rootDirectory,
                AllowedAddresses = allowedIPs.Select(IPAddress.Parse).ToArray(),
                AllowedAddressesSlots = allowedIPsSlots,
            });
            if (!string.IsNullOrEmpty(allowOrigin))
            {
                webServer.AllowOrigin = allowOrigin;
            }
            await webServer.RunAsync(cancellationToken: context.GetCancellationToken());
        });
    }
}
