using System.CommandLine;
using System.CommandLine.Binding;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;

namespace QueryCat.Cli.Commands.Options;

internal class QueryOptionsBinder : BinderBase<QueryOptions>
{
    internal static readonly TimeSpan FollowDefaultTimeout = TimeSpan.FromSeconds(2);

    private readonly Option<int> _maxErrorsOption;
    private readonly Option<bool> _statisticOption;
    private readonly Option<bool> _detailedStatisticOption;
    private readonly Option<bool> _rowNumberOption;
    private readonly Option<int> _pageSizeOption;
    private readonly Option<TextTableOutput.Style> _outputStyleOption;
    private readonly Option<int> _analyzeRowsOption;
    private readonly Option<string?> _columnsSeparatorOption;
    private readonly Option<bool> _disableCacheOption;
    private readonly Option<bool> _noHeaderOption;
    private readonly Option<string> _floatNumberOption;
    private readonly Option<bool> _followOption;
    private readonly Option<int> _tailOption;
    private readonly Option<int> _timeoutOption;
    private readonly Option<bool> _safeModeOption;

    public QueryOptionsBinder(
        Option<int> maxErrorsOption,
        Option<bool> statisticOption,
        Option<bool> detailedStatisticOption,
        Option<bool> rowNumberOption,
        Option<int> pageSizeOption,
        Option<TextTableOutput.Style> outputStyleOption,
        Option<int> analyzeRowsOption,
        Option<string?> columnsSeparatorOption,
        Option<bool> disableCacheOption,
        Option<bool> noHeaderOption,
        Option<string> floatNumberOption,
        Option<bool> followOption,
        Option<int> tailOption,
        Option<int> timeoutOption,
        Option<bool> safeModeOption)
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
        _columnsSeparatorOption = columnsSeparatorOption;
        _disableCacheOption = disableCacheOption;
        _noHeaderOption = noHeaderOption;
        _floatNumberOption = floatNumberOption;
        _followOption = followOption;
        _tailOption = tailOption;
        _timeoutOption = timeoutOption;
        _safeModeOption = safeModeOption;
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
            ColumnsSeparator = bindingContext.ParseResult.GetValueForOption(_columnsSeparatorOption),
            DisableCache = bindingContext.ParseResult.GetValueForOption(_disableCacheOption),
            NoHeader = bindingContext.ParseResult.GetValueForOption(_noHeaderOption),
            FloatNumberFormat = bindingContext.ParseResult.GetValueForOption(_floatNumberOption)
                ?? VariantValue.FloatNumberFormat,
            FollowTimeout = bindingContext.ParseResult.GetValueForOption(_followOption) ? FollowDefaultTimeout : TimeSpan.Zero,
            TailCount = bindingContext.ParseResult.GetValueForOption(_tailOption),
            QueryTimeout = TimeSpan.FromMilliseconds(bindingContext.ParseResult.GetValueForOption(_timeoutOption)),
            SafeMode = bindingContext.ParseResult.GetValueForOption(_safeModeOption),
        };
    }
}
