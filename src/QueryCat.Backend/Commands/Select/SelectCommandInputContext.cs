using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select;

internal class SelectCommandInputContext
{
    /// <summary>
    /// Rows input that is generated from function or subquery.
    /// </summary>
    public IRowsInput RowsInput { get; }

    public SelectInputQueryContext InputQueryContext { get; }

    public string Alias { get; set; }

    /// <summary>
    /// Was the input created from variable.
    /// </summary>
    public bool IsVariableBound { get; set; }

    public SelectCommandInputContext(IRowsInput rowsInput, SelectInputQueryContext inputQueryContext,
        string? alias = null)
    {
        RowsInput = rowsInput;
        InputQueryContext = inputQueryContext;
        Alias = alias ?? string.Empty;
    }

    public SelectCommandInputContext(IRowsInput rowsInput) : this(rowsInput, new SelectInputQueryContext(rowsInput))
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"alias={Alias}, input={RowsInput}";
}
