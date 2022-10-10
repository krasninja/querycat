using QueryCat.Backend.Logging;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Log messages to the console. Example: "[T] Trace message".
/// </summary>
public class ConsoleLogHandler : ILogHandler
{
    /// <inheritdoc />
    public void Log(LogItem logItem)
    {
        if (logItem.Level == LogLevel.Info)
        {
            Console.Out.WriteLine(logItem.Message);
        }
        else
        {
            if (logItem.Level >= LogLevel.Error)
            {
                Console.Error.WriteLine(LogItem.DefaultLogFormatter(logItem));
            }
            else
            {
                Console.Out.WriteLine(LogItem.DefaultLogFormatter(logItem));
            }
        }
    }
}
