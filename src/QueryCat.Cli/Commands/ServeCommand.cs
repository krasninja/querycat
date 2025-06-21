using System.CommandLine;
using System.Net;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

internal class ServeCommand : BaseCommand
{
    /// <inheritdoc />
    public ServeCommand() : base("serve", Resources.Messages.ServeCommand_Description)
    {
        var urlsOption = new Option<string>("--url")
        {
            Description = Resources.Messages.ServeCommand_UrlsDescription,
        };
        var allowOriginOption = new Option<string>("--allow-origin")
        {
            Description = Resources.Messages.ServeCommand_AllowOriginDescription,
        };
        var passwordOption = new Option<string>("--password")
        {
            Description = Resources.Messages.ServeCommand_PasswordDescription,
        };
        var rootDirectoryOption = new Option<string>("-r", "--root-dir")
        {
            Description = Resources.Messages.ServeCommand_RootDirectoryDescription,
        };
        var safeModeOption = new Option<bool>("--safe-mode")
        {
            Description = Resources.Messages.ServeCommand_SafeModeDescription,
        };
        var allowedIPsSlotsOption = new Option<int?>("--allowed-ips-slots")
        {
            Description = Resources.Messages.ServeCommand_AllowedIPsSlotsDescription,
        };
        var allowedIPsOption = new Option<string[]>("--allowed-ips")
        {
            Description = Resources.Messages.ServeCommand_AllowsIPsDescription,
            AllowMultipleArgumentsPerToken = true,
        };

        Add(urlsOption);
        Add(allowOriginOption);
        Add(passwordOption);
        Add(rootDirectoryOption);
        Add(safeModeOption);
        Add(allowedIPsOption);
        Add(allowedIPsSlotsOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var urls = parseResult.GetValue(urlsOption);
            var allowOrigin = parseResult.GetValue(allowOriginOption);
            var password = parseResult.GetValue(passwordOption) ?? string.Empty;
            var safeMode = parseResult.GetValue(safeModeOption);
            var rootDirectory = parseResult.GetValue(rootDirectoryOption) ?? string.Empty;
            var allowedIPs = parseResult.GetValue(allowedIPsOption) ?? [];
            var allowedIPsSlots = parseResult.GetValue(allowedIPsSlotsOption);

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
            await webServer.RunAsync(cancellationToken: cancellationToken);
        });
    }
}
