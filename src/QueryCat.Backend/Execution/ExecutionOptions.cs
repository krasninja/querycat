using System.Reflection;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Storage.Formats;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution options.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Default output if FROM is not specified.
    /// </summary>
    public IRowsOutput DefaultRowsOutput { get; set; } = NullRowsOutput.Instance;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PagingSize { get; set; } = 20;

    /// <summary>
    /// Output mode.
    /// </summary>
    public TextTableOutput.Style OutputStyle { get; set; } = TextTableOutput.Style.Table;

    /// <summary>
    /// Add row number to output.
    /// </summary>
    public bool AddRowNumberColumn { get; set; } = true;

    /// <summary>
    /// Show detailed statistic.
    /// </summary>
    public bool ShowDetailedStatistic { get; set; } = false;

    /// <summary>
    /// Max number of errors before abort.
    /// </summary>
    public int MaxErrors { get; set; } = -1;

    /// <summary>
    /// List of assemblies with additional functionality (functions, inputs, outputs, etc).
    /// </summary>
    public List<Assembly> PluginAssemblies { get; } = new();
}
