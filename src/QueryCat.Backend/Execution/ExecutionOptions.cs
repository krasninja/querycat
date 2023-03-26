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

    private int _pageSize = 20;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PagingSize
    {
        get => _pageSize;
        set
        {
            _pageSize = value;
            if (DefaultRowsOutput is PagingOutput pagingOutput)
            {
                pagingOutput.PagingRowsCount = _pageSize;
            }
        }
    }

    /// <summary>
    /// Add row number to output.
    /// </summary>
    public bool AddRowNumberColumn { get; set; }

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

    /// <summary>
    /// Run RC file (rc.sql).
    /// </summary>
    public bool RunBootstrapScript { get; set; } = true;

    /// <summary>
    /// Number of rows to analyze. This affects columns types detection and column length adjustment. -1 to analyze all.
    /// </summary>
    public int AnalyzeRowsCount { get; set; } = 10;

    /// <summary>
    /// Character to use to separate columns.
    /// </summary>
    public string? ColumnsSeparator { get; }

    /// <summary>
    /// Disable in-memory cache for sub-queries.
    /// </summary>
    public bool DisableCache { get; init; }

    public ExecutionOptions(
        TextTableOutput.Style outputStyle = TextTableOutput.Style.Table,
        string? columnsSeparator = null)
    {
        var tableOutput = new TextTableOutput(
            stream: StandardInputOutput.GetConsoleOutput(),
            separator: columnsSeparator,
            style: outputStyle);
        ColumnsSeparator = tableOutput.Separator;
        DefaultRowsOutput = new PagingOutput(tableOutput)
        {
            PagingRowsCount = PagingSize,
        };
    }
}
