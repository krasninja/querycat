using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.Cli;

[Command(Name = "qcat", Description = "The simple data query and transformation utility.",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.StopParsingAndCollect)]
[VersionOptionFromMember("--version", MemberName = nameof(InfoFunctions.GetVersion))]
[Subcommand(typeof(SchemaCommand))]
[Subcommand(typeof(AstCommand))]
[Subcommand(typeof(ExplainCommand))]
[HelpOption("-?|-h|--help")]
public class QueryCommand : BaseQueryCommand
{
    [Option("--stat", Description = "Show statistic")]
    protected bool ShowStatistic { get; } = false;

    // TODO: McMaster throws exception if this method is not defined.
    public static string GetVersion() => InfoFunctions.GetVersion();

    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        if (!app.Arguments.Any())
        {
            app.ShowHelp();
            return 0;
        }

        base.OnExecute(app, console);

        var runner = CreateRunner();
        runner.Run(Query);

        if (ShowStatistic)
        {
            console.WriteLine(new string('-', 5));
            console.WriteLine(runner.ExecutionThread.Statistic.ToString());
        }
        return 0;
    }
}
