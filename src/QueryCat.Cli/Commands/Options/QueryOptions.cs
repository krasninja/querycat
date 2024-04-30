using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;

namespace QueryCat.Cli.Commands.Options;

internal class QueryOptions
{
    public int MaxErrors { get; init; }

    public bool Statistic { get; init; }

    public bool DetailedStatistic { get; init; }

    public bool RowNumberOption { get; init; }

    public int PageSize { get; init; }

    public TextTableOutput.Style OutputStyle { get; init; }

    public int AnalyzeRows { get; init; }

    public string? ColumnsSeparator { get; init; }

    public bool DisableCache { get; init; }

    public bool NoHeader { get; init; }

    public string FloatNumberFormat { get; init; } = VariantValue.FloatNumberFormat;

    public TimeSpan FollowTimeout { get; init; }

    public int TailCount { get; init; } = -1;

    public TimeSpan QueryTimeout { get; init; }

    public bool SafeMode { get; init; }
}
