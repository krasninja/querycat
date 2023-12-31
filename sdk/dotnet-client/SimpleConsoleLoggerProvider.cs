using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Client;

internal sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
    private readonly List<IDisposable> _disposables = new(capacity: 32);

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        var dotIndex = categoryName.LastIndexOf('.');
        if (dotIndex > -1)
        {
            categoryName = categoryName.Substring(dotIndex + 1);
        }
        var logger = new SimpleConsoleLogger(categoryName);
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
