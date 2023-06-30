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
    /// Input arguments.
    /// </summary>
    public string[] InputArguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsInput">Rows input.</param>
    public QueryContextInputInfo(IRowsInput rowsInput)
    {
        RowsInput = rowsInput;
        RowsInputId = rowsInput.GetType().Name;
    }

    /// <summary>
    /// Set rows input arguments to distinct it among other queries of the same input.
    /// </summary>
    /// <param name="arguments">Input arguments (file name, ids).</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo SetInputArguments(params string[] arguments)
    {
        InputArguments = arguments;
        return this;
    }

    /// <summary>
    /// Merge rows input arguments to distinct it among other queries of the same input.
    /// </summary>
    /// <param name="arguments">Input arguments (file name, ids).</param>
    /// <returns>Instance of <see cref="QueryContextInputInfo" />.</returns>
    public QueryContextInputInfo MergeInputArguments(params string[] arguments)
    {
        if (arguments.Length > 0)
        {
            SetInputArguments(InputArguments.Concat(arguments).ToArray());
        }
        return this;
    }
}
