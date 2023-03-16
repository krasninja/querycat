using System.CommandLine;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Cli.Commands.Options;

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
            getDefaultValue: () => TextTableOutput.Style.Table);
        var analyzeRowsOption = new Option<int>("--analyze-rows",
            description: "Number of rows to analyze. -1 to analyze all.",
            getDefaultValue: () => 10);
        var columnsSeparatorOption = new Option<string?>("--columns-separator",
            description: "Character to use to separate columns.")
            {
                IsRequired = false,
            };

        this.AddOption(maxErrorsOption);
        this.AddOption(statisticOption);
        this.AddOption(detailedStatisticOption);
        this.AddOption(rowNumberOption);
        this.AddOption(pageSizeOption);
        this.AddOption(outputStyleOption);
        this.AddOption(analyzeRowsOption);
        this.AddOption(columnsSeparatorOption);
        this.SetHandler((applicationOptions, query, files, queryOptions) =>
        {
            applicationOptions.InitializeLogger();
            var options = new ExecutionOptions(queryOptions.OutputStyle, queryOptions.ColumnsSeparator)
            {
                AddRowNumberColumn = queryOptions.RowNumberOption,
                PagingSize = queryOptions.PageSize,
                ShowDetailedStatistic = queryOptions.DetailedStatistic,
                MaxErrors = queryOptions.MaxErrors,
                AnalyzeRowsCount = queryOptions.AnalyzeRows,
            };
            if (queryOptions.AnalyzeRows < 0)
            {
                options.AnalyzeRowsCount = int.MaxValue;
            }
            using var thread = applicationOptions.CreateExecutionThread(options);
            RunQuery(thread, query, files);

            if (queryOptions.Statistic || queryOptions.DetailedStatistic)
            {
                Console.WriteLine(new string('-', 5));
                Console.WriteLine(thread.Statistic.ToString());
            }
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            FilesOption,
            new QueryOptionsBinder(
                maxErrorsOption,
                statisticOption,
                detailedStatisticOption,
                rowNumberOption,
                pageSizeOption,
                outputStyleOption,
                analyzeRowsOption,
                columnsSeparatorOption)
            );
    }
}
