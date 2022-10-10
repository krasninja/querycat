namespace QueryCat.Backend.Logging;

/// <summary>
/// Simple logger implementation. It contains array of <see cref="ILogHandler" />
/// that is used for log items output. Use static <see cref="Instance" />.
/// </summary>
public sealed class Logger
{
    private ILogHandler[] _logHandlers = Array.Empty<ILogHandler>();

    /// <summary>
    /// Log handlers list.
    /// </summary>
    public IReadOnlyList<ILogHandler> LogHandlers => _logHandlers;

    /// <summary>
    /// Static instance of logger.
    /// </summary>
    public static Logger Instance { get; } = new();

    /// <summary>
    /// Private constructor. Use <see cref="Instance" /> property.
    /// </summary>
    private Logger()
    {
    }

    public void AddHandlers(params ILogHandler[] handlers)
    {
        _logHandlers = _logHandlers.Union(handlers).ToArray();
    }

    public void RemoveHandler(ILogHandler logHandler)
    {
        if (logHandler == null)
        {
            throw new ArgumentNullException(nameof(logHandler));
        }
        _logHandlers = _logHandlers.Except(new[] { logHandler }).ToArray();
    }

    /// <summary>
    /// Maximum log level.
    /// </summary>
    public LogLevel MaxLevel { get; set; } = LogLevel.Fatal;

    /// <summary>
    /// Minimum log level.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Is log level enabled.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <returns>True if log level currently enabled, false otherwise.</returns>
    public bool IsEnabled(LogLevel level) => level >= MinLevel && level <= MaxLevel;

    /// <summary>
    /// General log method.
    /// </summary>
    /// <param name="logItem">Log item to log.</param>
    public void Log(LogItem logItem)
    {
        if (!IsEnabled(logItem.Level))
        {
            return;
        }
        foreach (var logHandler in LogHandlers)
        {
            logHandler.Log(logItem);
        }
    }
}
