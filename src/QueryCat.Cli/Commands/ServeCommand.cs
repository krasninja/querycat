using System.CommandLine;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Storage;
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

internal class ServeCommand : BaseCommand
{
    /// <inheritdoc />
    public ServeCommand() : base("serve", "Run simple HTTP server.")
    {
        var urlsOption = new Option<string>("--url", description: "Endpoint to serve on.");

        this.AddOption(urlsOption);
        this.SetHandler((queryOptions, urls) =>
        {
            var executionThread = queryOptions.CreateExecutionThread(new ExecutionOptions
            {
                PagingSize = ExecutionOptions.NoLimit,
                AddRowNumberColumn = false,
                DefaultRowsOutput = NullRowsOutput.Instance,
            });
            var webServer = new WebServer(executionThread, urls);
            webServer.Run();
        }, new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), urlsOption);
    }
}
