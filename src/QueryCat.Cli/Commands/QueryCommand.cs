using System.CommandLine;
using QueryCat.Backend;
using QueryCat.Backend.Formatters;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

internal class QueryCommand : BaseQueryCommand
{
    public const string QueryCommandName = "query";

    internal static readonly TimeSpan FollowDefaultTimeout = TimeSpan.FromSeconds(2);

    public QueryCommand() : base(QueryCommandName, Resources.Messages.QueryCommand_Description)
    {
        var maxErrorsOption = new Option<int>("--max-errors")
        {
            Description = Resources.Messages.QueryCommand_MaxErrorsDescription,
            DefaultValueFactory = _ => -1,
        };
        var statisticOption = new Option<bool>("--stat")
        {
            Description = Resources.Messages.QueryCommand_StatisticDescription,
        };
        var detailedStatisticOption = new Option<bool>("--detailed-stat")
        {
            Description = Resources.Messages.QueryCommand_DetailedStatisticDescription,
        };
        var rowNumberOption = new Option<bool>("--row-number")
        {
            Description = Resources.Messages.QueryCommand_RowNumberDescription,
        };
        var pageSizeOption = new Option<int>("--page-size")
        {
            Description = Resources.Messages.QueryCommand_PageSizeDescription,
            DefaultValueFactory = _ => 20,
        };
        var analyzeRowsOption = new Option<int>("--analyze-rows")
        {
            Description = Resources.Messages.QueryCommand_AnalyzeRowsDescription,
            DefaultValueFactory = _ => 10,
        };
        var skipIfNoColumnsOption = new Option<bool>("--skip-if-no-columns")
        {
            Description = Resources.Messages.QueryCommand_SkipIfNoColumnsDescription,
        };
        var disableCacheOption = new Option<bool>("--disable-cache")
        {
            Description = Resources.Messages.QueryCommand_DisableCacheDescription,
        };
        var noHeaderOption = new Option<bool>("--no-header")
        {
            Description = Resources.Messages.QueryCommand_NoHeaderDescription,
        };
        var floatNumberOption = new Option<string>("--float-format")
        {
            Description = Resources.Messages.QueryCommand_FloatFormatDescription,
            DefaultValueFactory = _ => "F",
        };
        var followOption = new Option<bool>("--follow")
        {
            Description = Resources.Messages.QueryCommand_FollowDescription,
        };
        var tailOption = new Option<int>("--tail")
        {
            Description = Resources.Messages.QueryCommand_TailDescription,
            DefaultValueFactory = _ => -1,
        };
        var timeoutOption = new Option<int>("--timeout")
        {
            Description = Resources.Messages.QueryCommand_TimeoutDescription,
        };
        var safeModeOption = new Option<bool>("--safe-mode")
        {
            Description = Resources.Messages.QueryCommand_SafeModeDescription,
        };

        this.Add(maxErrorsOption);
        this.Add(statisticOption);
        this.Add(detailedStatisticOption);
        this.Add(rowNumberOption);
        this.Add(pageSizeOption);
        this.Add(analyzeRowsOption);
        this.Add(skipIfNoColumnsOption);
        this.Add(disableCacheOption);
        this.Add(noHeaderOption);
        this.Add(floatNumberOption);
        this.Add(followOption);
        this.Add(tailOption);
        this.Add(timeoutOption);
        this.Add(safeModeOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var files = parseResult.GetValue(FilesOption);

            applicationOptions.InitializeLogger();
            var tableOutput = new TextTableOutput(
                stream: Stdio.GetConsoleOutput(),
                hasHeader: !parseResult.GetValue(noHeaderOption),
                separator: parseResult.GetValue(ColumnsSeparatorOption),
                style: parseResult.GetValue(OutputStyleOption),
                floatNumberFormat: parseResult.GetValue(floatNumberOption));
            var options = new AppExecutionOptions
            {
                AddRowNumberColumn = parseResult.GetValue(rowNumberOption),
                ShowDetailedStatistic = parseResult.GetValue(detailedStatisticOption),
                MaxErrors = parseResult.GetValue(maxErrorsOption),
                AnalyzeRowsCount = parseResult.GetValue(analyzeRowsOption),
                SkipIfNoColumns = parseResult.GetValue(skipIfNoColumnsOption),
                DisableCache = parseResult.GetValue(disableCacheOption),
                UseConfig = true,
                RunBootstrapScript = true,
                FollowTimeout = parseResult.GetValue(followOption) ? FollowDefaultTimeout : TimeSpan.Zero,
                TailCount = parseResult.GetValue(tailOption),
                QueryTimeout = TimeSpan.FromMilliseconds(parseResult.GetValue(timeoutOption)),
                SafeMode = parseResult.GetValue(safeModeOption),
            };
            if (parseResult.GetValue(analyzeRowsOption) < 0)
            {
                options.AnalyzeRowsCount = int.MaxValue;
            }

            await using var root = await applicationOptions.CreateApplicationRootAsync(options);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            options.DefaultRowsOutput = new PagingOutput(tableOutput, cancellationTokenSource: cts)
            {
                PagingRowsCount = parseResult.GetValue(pageSizeOption),
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, query, files, cts.Token);

            if (parseResult.GetValue(statisticOption) || parseResult.GetValue(detailedStatisticOption))
            {
                Console.WriteLine(new string('-', 5));
                Console.WriteLine(root.Thread.Statistic.Dump());
            }
        });
    }
}
