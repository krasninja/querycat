using System.CommandLine;
using QueryCat.Mcp;

namespace QueryCat.Cli.Commands;

internal sealed class McpCommand : BaseCommand
{
    private enum McpTransport
    {
        Http,
        Stdio
    }

    /// <inheritdoc />
    public McpCommand() : base("mcp", Resources.Messages.McpCommand_Description)
    {
        var urlOption = new Option<string>("--url")
        {
            Description = Resources.Messages.ServeCommand_UrlsDescription,
        };
        var transportOption = new Option<McpTransport>("--transport")
        {
            Description = Resources.Messages.McpCommand_TransportDescription,
            DefaultValueFactory = _ => McpTransport.Http,
        };
        var safeModeOption = new Option<bool>("--safe-mode")
        {
            Description = Resources.Messages.ServeCommand_SafeModeDescription,
        };

        Add(urlOption);
        Add(transportOption);
        Add(safeModeOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var urls = parseResult.GetValue(urlOption);
            var transport = parseResult.GetValue(transportOption);
            var safeMode = parseResult.GetValue(safeModeOption);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.Options.SafeMode = safeMode;
            var mcpTransport = CreateTransport(new McpServerOptions
            {
                Url = urls,
            }, transport);
            var mcpServer = new McpServer(root.Thread, mcpTransport);
            await mcpTransport.StartAsync(cancellationToken: cancellationToken);
            await mcpServer.LoopAsync(cancellationToken);
        });
    }

    private IMcpTransport CreateTransport(McpServerOptions options, McpTransport transport)
    {
        if (transport == McpTransport.Stdio)
        {
            return new McpStdioTransport();
        }
        return new McpWebTransport(options);
    }
}
