using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Commands.Select;

internal class SelectCommandContextInput
{
    /// <summary>
    /// Rows input that is generated from function or subquery.
    /// </summary>
    public IRowsInput RowsInput { get; }

    public SelectInputQueryContext InputQueryContext { get; }

    public string Alias { get; set; }

    public HashSet<int> PrefetchColumnsIds { get; } = new();

    public SelectCommandContextInput(IRowsInput rowsInput, SelectInputQueryContext inputQueryContext, string? alias = null)
    {
        RowsInput = rowsInput;
        InputQueryContext = inputQueryContext;
        Alias = alias ?? string.Empty;
    }

    public SelectCommandContextInput(IRowsInput rowsInput) : this(rowsInput, new SelectInputQueryContext(rowsInput))
    {
    }
}
