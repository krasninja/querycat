using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Client.Logging;

internal sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;
    private readonly List<IDisposable> _disposables = new(capacity: 32);

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
        _disposables.Add(logger);
        return logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }
}
