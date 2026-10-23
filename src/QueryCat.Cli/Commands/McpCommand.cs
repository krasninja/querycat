using System.CommandLine;
using QueryCat.Cli.Infrastructure;
using QueryCat.Mcp;
using QueryCat.Mcp.Transports;

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
        var allowOriginOption = new Option<string>("--allow-origin")
        {
            Description = Resources.Messages.ServeCommand_AllowOriginDescription,
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
        Add(allowOriginOption);
        Add(transportOption);
        Add(safeModeOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var urls = parseResult.GetValue(urlOption);
            var allowOrigin = parseResult.GetValue(allowOriginOption);
            var transport = parseResult.GetValue(transportOption);
            var safeMode = parseResult.GetValue(safeModeOption);

            applicationOptions.InitializeLogger(useErrorOut: transport == McpTransport.Stdio);
            await using var root = await applicationOptions.CreateApplicationRootAsync(new AppExecutionOptions
            {
                SafeMode = safeMode,
                RunBootstrapScript = true,
                UseConfig = true,
            });
            var mcpTransport = CreateTransport(new McpServerOptions
            {
                Url = urls,
                AllowOrigin = allowOrigin,
            }, transport);
            var mcpServer = new McpServer(root.Thread, mcpTransport);
            McpServerInitializer.AddDefaultTools(mcpServer);
            await mcpTransport.StartAsync(cancellationToken: cancellationToken);

            // Loop.
            try
            {
                await mcpServer.LoopAsync(cancellationToken);
            }
            finally
            {
                await mcpTransport.StopAsync(cancellationToken);
                if (mcpTransport is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (mcpTransport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        });
    }

    private static IMcpTransport CreateTransport(McpServerOptions options, McpTransport transport)
    {
        if (transport == McpTransport.Stdio)
        {
            return new McpStdioTransport();
        }
        return new McpHttpTransport(options);
    }
}
