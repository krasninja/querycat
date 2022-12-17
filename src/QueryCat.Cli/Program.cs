using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Text;
using Serilog;
using QueryCat.Backend;
using QueryCat.Cli.Commands;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
internal class Program
{
    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">Application execution arguments.</param>
    /// <returns>Error code.</returns>
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        if (args.Length < 1)
        {
            args = new[] { "-h" };
        }

        // Root.
        var rootCommand = new ApplicationRootCommand
        {
            new QueryCommand(),
            new ExplainCommand(),
            new AstCommand(),
            new SchemaCommand(),
            new ServeCommand(),
#if ENABLE_PLUGINS
            new Command("plugin", "Plugins management.")
            {
                new PluginInstallCommand(),
                new PluginListCommand(),
                new PluginRemoveCommand(),
                new PluginUpdateCommand(),
            },
#endif
        };
        rootCommand.TreatUnmatchedTokensAsErrors = false;

        // Allow to query without "query" command. Fast way.
        var queryArgument = new Argument<string>("query")
        {
            IsHidden = true,
        };
        rootCommand.AddArgument(queryArgument);
        rootCommand.SetHandler(_ =>
        {
            new QueryCommand().Invoke(args);
        }, queryArgument);

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseHelp(context =>
            {
                if (context.Command is RootCommand)
                {
                    var layouts = HelpBuilder.Default.GetLayout().ToList();
                    layouts.Insert(1, _ =>
                    {
                        var sb = new StringBuilder()
                            .AppendLine("Getting started:")
                            .AppendLine()
                            .AppendLine("  -- Simple select from CSV file")
                            .AppendLine("  qcat \"select * from '/home/user/users.csv' where email like '%@gmail.com'\"")
                            .AppendLine()
                            .AppendLine("  Visit https://github.com/krasninja/querycat for more information.");
                        context.Output.Write(sb);
                    });
                    context.HelpBuilder.CustomizeLayout(_ => layouts);
                }
            })
            .Build();
        return parser.Invoke(args);
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        switch (e.ExceptionObject)
        {
            case QueryCatException queryCatException:
                Log.Logger.Error(queryCatException.Message);
                break;
            case Exception exception:
                Log.Logger.Fatal(exception, exception.Message);
                break;
        }
        Environment.Exit(1);
    }
}
