namespace QueryCat.Backend.Logging;

/// <summary>
/// Log item for logging.
/// </summary>
public readonly struct LogItem
{
    /// <summary>
    /// Log level.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Message source.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Exception to log.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Log level one letter code.
    /// </summary>
    public char LogLevelCode => Level switch
    {
        LogLevel.Debug => 'D',
        LogLevel.Trace => 'T',
        LogLevel.Info => 'I',
        LogLevel.Warning => 'W',
        LogLevel.Error => 'E',
        LogLevel.Fatal => 'F',
        _ => ' '
    };

    /// <summary>
    /// Default log formatter delegate.
    /// </summary>
    /// <param name="logItem">Log item to generate a text string.</param>
    /// <returns>Text string.</returns>
    public static ReadOnlySpan<char> DefaultLogFormatter(LogItem logItem)
    {
        ReadOnlySpan<char> message;
        if (string.IsNullOrEmpty(logItem.Source))
        {
            message = $"[{logItem.LogLevelCode}] {logItem.Message}";
        }
        else
        {
            message = $"[{logItem.LogLevelCode}] {logItem.Source}: {logItem.Message}";
        }
        if (logItem.Exception != null)
        {
            message = string.Concat(message, logItem.Exception.Message);
        }
        return message;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="source">Message source. Optional.</param>
    /// <param name="exception">Exception to log. Optional.</param>
    public LogItem(LogLevel level, string message, string? source = null, Exception? exception = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
        Source = source ?? string.Empty;
    }
}
