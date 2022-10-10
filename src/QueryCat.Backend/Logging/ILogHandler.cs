namespace QueryCat.Backend.Logging;

/// <summary>
/// Log handler is the output provider to log messages.
/// </summary>
public interface ILogHandler
{
    /// <summary>
    /// Render log message.
    /// </summary>
    /// <param name="logItem">Log item to render.</param>
    void Log(LogItem logItem);
}
