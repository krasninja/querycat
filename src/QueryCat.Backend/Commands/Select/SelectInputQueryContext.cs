using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// Context for input rows source.
/// </summary>
internal sealed class SelectInputQueryContext : QueryContext
{
#if DEBUG
    private readonly int _id = IdGenerator.GetNext(3000);
#endif

    /// <summary>
    /// The target rows input.
    /// </summary>
    public IRowsInput RowsInput { get; set; }

    /// <summary>
    /// Input alternative name.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Was the input created from variable.
    /// </summary>
    public bool IsVariableBound { get; set; }

    /// <summary>
    /// Is vary input source. Vary input source contain identifiers as input arguments.
    /// </summary>
    public bool IsVary { get; set; }

    /// <inheritdoc />
    public SelectInputQueryContext(IRowsInput rowsInput, Column[] columns, IConfigStorage configStorage)
        : base(new QueryContextQueryInfo(columns))
    {
        RowsInput = rowsInput;
        ConfigStorage = configStorage;
    }

    /// <inheritdoc />
    public SelectInputQueryContext(IRowsInput rowsInput, ExecutionOptions options)
        : this(rowsInput, rowsInput.Columns, NullConfigStorage.Instance)
    {
        PrereadRowsCount = options.AnalyzeRowsCount;
        SkipIfNoColumns = options.SkipIfNoColumns;
    }
}
