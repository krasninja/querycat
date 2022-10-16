using McMaster.Extensions.CommandLineUtils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Storage.Formats;

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
    [Option("--stat", Description = "Show statistic.")]
    protected bool ShowStatistic { get; } = false;

    [Option("--max-errors", Description = "Max number of errors before abort. -1 is ignore.")]
    protected int MaxErrors { get; } = -1;

    [Option("--detailed-stat", Description = "Show detailed statistic (include rows errors).")]
    protected bool ShowDetailedStatistic { get; } = false;

    [Option("--row-number", Description = "Include row number column.")]
    public bool RowNumber { get; } = false;

    [Option("--page-size", Description = "Output page size. Set -1 to show all.")]
    public int PageSize { get; } = 20;

    [Option("--output-style", Description = "Output style.")]
    public TextTableOutput.Style OutputStyle { get; set; } = TextTableOutput.Style.Table;

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
        var runner = CreateRunner(new ExecutionOptions
        {
            AddRowNumberColumn = RowNumber,
            PagingSize = PageSize,
            OutputStyle = OutputStyle,
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
