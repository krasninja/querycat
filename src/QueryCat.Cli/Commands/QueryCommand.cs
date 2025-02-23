using System.CommandLine;
using QueryCat.Backend;
using QueryCat.Backend.Formatters;
using QueryCat.Cli.Commands.Options;
using QueryCat.Cli.Infrastructure;

namespace QueryCat.Cli.Commands;

internal class QueryCommand : BaseQueryCommand
{
    public const string QueryCommandName = "query";

    public QueryCommand() : base(QueryCommandName, "Execute SQL query.")
    {
        var maxErrorsOption = new Option<int>("--max-errors",
            description: "Max number of errors before abort. -1 is ignore.",
            getDefaultValue: () => -1);
        var statisticOption = new Option<bool>("--stat",
            description: "Show statistic.",
            getDefaultValue: () => false);
        var detailedStatisticOption = new Option<bool>("--detailed-stat",
            description: "Show detailed statistic (include rows errors).",
            getDefaultValue: () => false);
        var rowNumberOption = new Option<bool>("--row-number",
            description: "Include row number column.",
            getDefaultValue: () => false);
        var pageSizeOption = new Option<int>("--page-size",
            description: "Output page size. Set -1 to show all.",
            getDefaultValue: () => 20);
        var outputStyleOption = new Option<TextTableOutput.Style>("--output-style",
            description: "Output style.",
            getDefaultValue: () => TextTableOutput.Style.Table1);
        var analyzeRowsOption = new Option<int>("--analyze-rows",
            description: "Number of rows to analyze. -1 to analyze all.",
            getDefaultValue: () => 10);
        var columnsSeparatorOption = new Option<string?>("--columns-separator",
            description: "Character to use to separate columns.")
            {
                IsRequired = false,
            };
        var disableCacheOption = new Option<bool>("--disable-cache",
            description: "Do not use memory cache for sub-queries.");
        var noHeaderOption = new Option<bool>("--no-header",
            description: "Do not render header.");
        var floatNumberOption = new Option<string>("--float-format",
            description: "Float numbers format.",
            getDefaultValue: () => "F");
        var followOption = new Option<bool>("--follow",
            description: "Output appended data as the input source grows.");
        var tailOption = new Option<int>("--tail",
            description: "Return the specific number of rows from tail.",
            getDefaultValue: () => -1);
        var timeoutOption = new Option<int>("--timeout",
            description: "Query timeout in ms.");
        var safeModeOption = new Option<bool>("--safe-mode",
            description: "Allow to call only safe (no modifications) functions.");

        this.AddOption(maxErrorsOption);
        this.AddOption(statisticOption);
        this.AddOption(detailedStatisticOption);
        this.AddOption(rowNumberOption);
        this.AddOption(pageSizeOption);
        this.AddOption(outputStyleOption);
        this.AddOption(analyzeRowsOption);
        this.AddOption(columnsSeparatorOption);
        this.AddOption(disableCacheOption);
        this.AddOption(noHeaderOption);
        this.AddOption(floatNumberOption);
        this.AddOption(followOption);
        this.AddOption(tailOption);
        this.AddOption(timeoutOption);
        this.AddOption(safeModeOption);
        this.SetHandler(async (context) =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var query = OptionsUtils.GetValueForOption(QueryArgument, context);
            var variables = OptionsUtils.GetValueForOption(VariablesOption, context);
            var files = OptionsUtils.GetValueForOption(FilesOption, context);
            var queryOptions = OptionsUtils.GetValueForOption(new QueryOptionsBinder(
                maxErrorsOption,
                statisticOption,
                detailedStatisticOption,
                rowNumberOption,
                pageSizeOption,
                outputStyleOption,
                analyzeRowsOption,
                columnsSeparatorOption,
                disableCacheOption,
                noHeaderOption,
                floatNumberOption,
                followOption,
                tailOption,
                timeoutOption,
                safeModeOption), context);

            applicationOptions.InitializeLogger();
            var tableOutput = new TextTableOutput(
                stream: Stdio.GetConsoleOutput(),
                hasHeader: !queryOptions.NoHeader,
                separator: queryOptions.ColumnsSeparator,
                style: queryOptions.OutputStyle,
                floatNumberFormat: queryOptions.FloatNumberFormat);
            var options = new AppExecutionOptions
            {
                AddRowNumberColumn = queryOptions.RowNumberOption,
                ShowDetailedStatistic = queryOptions.DetailedStatistic,
                MaxErrors = queryOptions.MaxErrors,
                AnalyzeRowsCount = queryOptions.AnalyzeRows,
                DisableCache = queryOptions.DisableCache,
                UseConfig = true,
                RunBootstrapScript = true,
                FollowTimeout = queryOptions.FollowTimeout,
                TailCount = queryOptions.TailCount,
                QueryTimeout = queryOptions.QueryTimeout,
                SafeMode = queryOptions.SafeMode,
            };
            if (queryOptions.AnalyzeRows < 0)
            {
                options.AnalyzeRowsCount = int.MaxValue;
            }
            using var root = await applicationOptions.CreateApplicationRootAsync(options);
            options.DefaultRowsOutput = new PagingOutput(tableOutput, cancellationTokenSource: root.CancellationTokenSource)
            {
                PagingRowsCount = queryOptions.PageSize,
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, query, files, root.CancellationTokenSource.Token);

            if (queryOptions.Statistic || queryOptions.DetailedStatistic)
            {
                Console.WriteLine(new string('-', 5));
                Console.WriteLine(root.Thread.Statistic.Dump());
            }
        });
    }
}
