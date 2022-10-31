using System.Reflection;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Providers;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution options.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Default output if FROM is not specified.
    /// </summary>
    public IRowsOutput DefaultRowsOutput { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PagingSize { get; set; } = 20;

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

    public ExecutionOptions(TextTableOutput.Style outputStyle = TextTableOutput.Style.Table)
    {
        DefaultRowsOutput = new TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            style: outputStyle);
    }
}
