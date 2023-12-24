using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution options.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Default output target if INTO clause is not specified.
    /// </summary>
    public IRowsOutput DefaultRowsOutput { get; set; } = NullRowsOutput.Instance;

    /// <summary>
    /// Add row number to output.
    /// </summary>
    public bool AddRowNumberColumn { get; set; }

    /// <summary>
    /// Show detailed statistic.
    /// </summary>
    public bool ShowDetailedStatistic { get; set; }

    /// <summary>
    /// Max number of errors before query abort.
    /// </summary>
    public int MaxErrors { get; set; } = -1;

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
    public bool UseConfig { get; set; }

    /// <summary>
    /// Run RC file (rc.sql).
    /// </summary>
    public bool RunBootstrapScript { get; set; }

    /// <summary>
    /// Number of rows to analyze. This affects columns types detection and column length adjustment. -1 to analyze all.
    /// </summary>
    public int AnalyzeRowsCount { get; set; } = 10;

    /// <summary>
    /// Disable in-memory cache for sub-queries.
    /// </summary>
    public bool DisableCache { get; init; }

    /// <summary>
    /// Write appended data as source grows. Specifies check timeout. 0 means do not follow.
    /// </summary>
    public int FollowTimeoutMs { get; init; }
}
