using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Commands.Select;

internal class SelectCommandInputContext
{
    /// <summary>
    /// Rows input that is generated from function or subquery.
    /// </summary>
    public IRowsInput RowsInput { get; }

    public SelectInputQueryContext InputQueryContext { get; }

    public string Alias { get; set; }

    public SelectCommandInputContext(IRowsInput rowsInput, SelectInputQueryContext inputQueryContext,
        string? alias = null)
    {
        RowsInput = rowsInput;
        InputQueryContext = inputQueryContext;
        Alias = alias ?? string.Empty;
    }

    public SelectCommandInputContext(IRowsInput rowsInput, IExecutionThread executionThread)
        : this(rowsInput, new SelectInputQueryContext(rowsInput, executionThread))
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"alias={Alias}, input={RowsInput}";
}
