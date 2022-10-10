using System.Diagnostics;

namespace QueryCat.Backend.Logging;

/// <summary>
/// Extensions for <see cref="Logger" />.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Log debug. Available only for debug builds.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    [Conditional("DEBUG")]
    public static void Debug(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Debug, message, source, exception));

    /// <summary>
    /// Log trace.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void Trace(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Trace, message, source, exception));

    /// <summary>
    /// Log information.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void Info(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Info, message, source, exception));

    /// <summary>
    /// Log warning.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void Warning(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Warning, message, source, exception));

    /// <summary>
    /// Log error.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void Error(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Error, message, source, exception));

    /// <summary>
    /// Log fatal error.
    /// </summary>
    /// <param name="logger"><see cref="Logger" /> instance.</param>
    /// <param name="message">Log message.</param>
    /// <param name="source">Log source.</param>
    /// <param name="exception">Optional exception to log.</param>
    public static void Fatal(this Logger logger, string message, string? source = null, Exception? exception = null)
        => logger.Log(new LogItem(LogLevel.Fatal, message, source, exception));
}
