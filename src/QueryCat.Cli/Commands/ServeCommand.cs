using System.CommandLine;
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

        AddOption(urlsOption);
        AddOption(allowOriginOption);
        AddOption(passwordOption);
        this.SetHandler((applicationOptions, urls, allowOrigin, password) =>
        {
            applicationOptions.InitializeLogger();
            using var root = applicationOptions.CreateApplicationRoot();
            root.Thread.Options.AddRowNumberColumn = true;
            var webServer = new WebServer(root.Thread, urls, password);
            if (!string.IsNullOrEmpty(allowOrigin))
            {
                webServer.AllowOrigin = allowOrigin;
            }
            webServer.Run();
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            urlsOption,
            allowOriginOption,
            passwordOption);
    }
}
