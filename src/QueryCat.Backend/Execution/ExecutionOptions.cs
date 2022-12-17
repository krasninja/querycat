using System.Reflection;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Providers;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution options.
/// </summary>
public sealed class ExecutionOptions
{
    public const int NoLimit = -1;

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
    public bool ShowDetailedStatistic { get; set; }

    /// <summary>
    /// Max number of errors before abort.
    /// </summary>
    public int MaxErrors { get; set; } = -1;

    /// <summary>
    /// List of assemblies with additional functionality (functions, inputs, outputs, etc).
    /// </summary>
    public List<Assembly> PluginAssemblies { get; } = new();

#if ENABLE_PLUGINS
    /// <summary>
    /// List of directories to search for plugins.
    /// </summary>
    public List<string> PluginDirectories { get; } = new();

    /// <summary>
    /// Plugins repository. If empty - default will be used.
    /// </summary>
    public string? PluginsRepositoryUri { get; init; }
#endif

    /// <summary>
    /// Do not save/load config.
    /// </summary>
    public bool UseConfig { get; set; } = true;

    public ExecutionOptions(TextTableOutput.Style outputStyle = TextTableOutput.Style.Table)
    {
        var tableOutput = new TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            style: outputStyle);
        DefaultRowsOutput = new PagingOutput(tableOutput)
        {
            PagingRowsCount = PagingSize,
        };
    }
}
