using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query context input info.
/// </summary>
public sealed class QueryContextInputInfo
{
    /// <summary>
    /// Specific rows input.
    /// </summary>
    public IRowsInput RowsInput { get; }

    /// <summary>
    /// Rows input identifier (class name, function name, etc).
    /// </summary>
    public string RowsInputId { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsInput">Rows input.</param>
    public QueryContextInputInfo(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
        RowsInputId = rowsInput.GetType().Name;
    }
}
