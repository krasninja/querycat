using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.Cli;

[Command(Name = "qcat", Description = "The simple data query and transformation utility.",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw)]
[VersionOptionFromMember("--version", MemberName = nameof(InfoFunctions.GetVersion))]
[Subcommand(
    typeof(SchemaCommand),
    typeof(AstCommand),
    typeof(ExplainCommand),
    typeof(ServeCommand))]
[HelpOption("-?|-h|--help")]
public class QueryCommand : BaseQueryCommand
{
    [Option("--stat", Description = "Show statistic.")]
    public bool ShowStatistic { get; } = false;

    [Option("--max-errors", Description = "Max number of errors before abort. -1 is ignore.")]
    public int MaxErrors { get; } = -1;

    [Option("--detailed-stat", Description = "Show detailed statistic (include rows errors).")]
    public bool ShowDetailedStatistic { get; } = false;

    [Option("--row-number", Description = "Include row number column.")]
    public bool RowNumber { get; } = false;

    [Option("--page-size", Description = "Output page size. Set -1 to show all.")]
    public int PageSize { get; } = 20;

    [Option("--output-style", Description = "Output style.")]
    public TextTableOutput.Style OutputStyle { get; } = TextTableOutput.Style.Table;

    // TODO: McMaster throws exception if this method is not defined.
    public static string GetVersion() => InfoFunctions.GetVersion();

    /// <inheritdoc />
    public override int OnExecute(CommandLineApplication app, IConsole console)
    {
        PreInitialize();
        OnExecuteInternal(console);
        return 0;
    }

    internal void OnExecuteInternal(IConsole console)
    {
        var runner = CreateRunner(new ExecutionOptions(OutputStyle)
        {
            AddRowNumberColumn = RowNumber,
            PagingSize = PageSize,
            ShowDetailedStatistic = ShowDetailedStatistic,
            MaxErrors = MaxErrors,
        });
        runner.Run(Query);

        if (ShowStatistic || ShowDetailedStatistic)
        {
            console.WriteLine(new string('-', 5));
            console.WriteLine(runner.ExecutionThread.Statistic.ToString());
        }
    }
}
