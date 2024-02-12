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
        AddOption(allowedIPsOption);
        AddOption(allowedIPsSlotsOption);
        this.SetHandler((applicationOptions, urls, allowOrigin, password, rootDirectory, allowedIPs, allowedIPsSlots) =>
        {
            applicationOptions.InitializeLogger();
            using var root = applicationOptions.CreateApplicationRoot();
            root.Thread.Options.AddRowNumberColumn = true;
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
            webServer.Run();
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            urlsOption,
            allowOriginOption,
            passwordOption,
            rootDirectoryOption,
            allowedIPsOption,
            allowedIPsSlotsOption);
    }
}
