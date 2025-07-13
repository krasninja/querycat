using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend;

/// <summary>
/// Execution options.
/// </summary>
public class ExecutionOptions
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

    /// <summary>
    /// Do not save/load config.
    /// </summary>
    public bool UseConfig { get; set; }

    /// <summary>
    /// Run RC file (rc.sql).
    /// </summary>
    public bool RunBootstrapScript { get; set; }

    /// <summary>
    /// Can execute only safe functions.
    /// </summary>
    public bool SafeMode { get; set; }

    /// <summary>
    /// Number of rows to analyze. This affects columns types detection and column length adjustment. -1 to analyze all.
    /// </summary>
    public int AnalyzeRowsCount { get; set; } = 10;

    /// <summary>
    /// Skip read if columns cannot be detected.
    /// </summary>
    internal bool SkipIfNoColumns { get; set; }

    /// <summary>
    /// Disable in-memory cache for sub-queries.
    /// </summary>
    public bool DisableCache { get; set; }

    /// <summary>
    /// Write appended data as source grows. Specifies check timeout. 0 means do not follow.
    /// </summary>
    public TimeSpan FollowTimeout { get; set; }

    /// <summary>
    /// Select specific number of source items from tail. -1 to disable.
    /// </summary>
    public int TailCount { get; set; } = -1;

    /// <summary>
    /// Throw time out exception if query hasn't been executed within the time.
    /// </summary>
    public TimeSpan QueryTimeout { get; set; }

    /// <summary>
    /// Maximum recursion depth.
    /// </summary>
    public int MaxRecursionDepth { get; set; } = 1024;

    /// <summary>
    /// Max number of completion to return.
    /// </summary>
    public int CompletionsCount { get; set; } = 20;

    /// <summary>
    /// Lock execution thread while it is being used by another caller.
    /// </summary>
    public bool PreventConcurrentRun { get; set; } = true;

    /// <summary>
    /// Define the array of allowed commands to execute.
    /// </summary>
    public string[]? AllowedCommands { get; set; }
}
