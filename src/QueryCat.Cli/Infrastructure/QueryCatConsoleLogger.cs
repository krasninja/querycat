using Microsoft.Extensions.Logging;
using QueryCat.Backend;

namespace QueryCat.Cli.Infrastructure;

internal sealed class QueryCatConsoleLogger : ILogger, IDisposable
{
    private readonly string _name;
    private readonly StreamWriter _streamWriter;

    public QueryCatConsoleLogger(string name)
    {
        _name = name;
        _streamWriter = new StreamWriter(Stdio.GetConsoleOutput());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "FAL",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        TextWriter writer = _streamWriter;
        if (logLevel >= LogLevel.Error)
        {
            writer = Console.Error;
        }

        writer.Write(GetLogLevelString(logLevel));
        if (!string.IsNullOrEmpty(_name) && logLevel != LogLevel.Information
            && logLevel != LogLevel.Error)
        {
            writer.Write(' ');
            writer.Write(_name);
        }
        writer.Write(": ");
        writer.WriteLine(formatter.Invoke(state, exception));
        if (logLevel >= LogLevel.Error && exception != null)
        {
            writer.WriteLine(exception);
        }
        writer.Flush();
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    /// <inheritdoc />
    public void Dispose()
    {
        _streamWriter.Dispose();
    }
}
