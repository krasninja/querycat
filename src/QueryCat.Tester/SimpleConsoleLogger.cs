using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace QueryCat.Tester;

/// <summary>
/// A logger that writes messages in the console.
/// </summary>
[UnsupportedOSPlatform("browser")]
internal sealed class SimpleConsoleLogger : ILogger, IDisposable
{
    private readonly string _name;
    private readonly TextWriter _streamWriter;

    public SimpleConsoleLogger(string name)
    {
        _name = name;
        _streamWriter = Console.Out;
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "T",
            LogLevel.Debug => "D",
            LogLevel.Information => "I",
            LogLevel.Warning => "W",
            LogLevel.Error => "E",
            LogLevel.Critical => "F",
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
        if (!string.IsNullOrEmpty(_name)
            && logLevel != LogLevel.Information
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
