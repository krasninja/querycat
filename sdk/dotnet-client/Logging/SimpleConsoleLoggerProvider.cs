using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Client.Logging;

internal sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;

    public SimpleConsoleLoggerProvider(LogLevel minLevel = LogLevel.Trace)
    {
        _minLevel = minLevel;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        var dotIndex = categoryName.LastIndexOf('.');
        if (dotIndex > -1)
        {
            categoryName = categoryName.Substring(dotIndex + 1);
        }
        var logger = new SimpleConsoleLogger(categoryName, _minLevel);
        return logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
