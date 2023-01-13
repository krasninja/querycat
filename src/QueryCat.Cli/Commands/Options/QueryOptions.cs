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
}
