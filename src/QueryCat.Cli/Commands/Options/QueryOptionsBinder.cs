using System.CommandLine;
using System.CommandLine.Binding;
using QueryCat.Backend.Formatters;

namespace QueryCat.Cli.Commands.Options;

internal class QueryOptionsBinder : BinderBase<QueryOptions>
{
    private readonly Option<int> _maxErrorsOption;
    private readonly Option<bool> _statisticOption;
    private readonly Option<bool> _detailedStatisticOption;
    private readonly Option<bool> _rowNumberOption;
    private readonly Option<int> _pageSizeOption;
    private readonly Option<TextTableOutput.Style> _outputStyleOption;
    private readonly Option<int> _analyzeRowsOption;

    public QueryOptionsBinder(
        Option<int> maxErrorsOption,
        Option<bool> statisticOption,
        Option<bool> detailedStatisticOption,
        Option<bool> rowNumberOption,
        Option<int> pageSizeOption, Option<TextTableOutput.Style> outputStyleOption,
        Option<int> analyzeRowsOption)
    {
        _maxErrorsOption = maxErrorsOption;
        _statisticOption = statisticOption;
        _detailedStatisticOption = detailedStatisticOption;
        _rowNumberOption = rowNumberOption;
        _pageSizeOption = pageSizeOption;
        _pageSizeOption.AddValidator(result =>
        {
            if (result.GetValueForOption(_pageSizeOption) < -1)
            {
                result.ErrorMessage = "Must be greater than 0.";
            }
        });
        _outputStyleOption = outputStyleOption;
        _analyzeRowsOption = analyzeRowsOption;
    }

    /// <inheritdoc />
    protected override QueryOptions GetBoundValue(BindingContext bindingContext)
    {
        return new QueryOptions
        {
            MaxErrors = bindingContext.ParseResult.GetValueForOption(_maxErrorsOption),
            Statistic = bindingContext.ParseResult.GetValueForOption(_statisticOption),
            DetailedStatistic = bindingContext.ParseResult.GetValueForOption(_detailedStatisticOption),
            RowNumberOption = bindingContext.ParseResult.GetValueForOption(_rowNumberOption),
            PageSize = bindingContext.ParseResult.GetValueForOption(_pageSizeOption),
            OutputStyle = bindingContext.ParseResult.GetValueForOption(_outputStyleOption),
            AnalyzeRows = bindingContext.ParseResult.GetValueForOption(_analyzeRowsOption),
        };
    }
}
