using Microsoft.Extensions.Logging;

namespace QueryCat.Cli.Infrastructure;

internal sealed class QueryCatConsoleLoggerProvider : ILoggerProvider
{
    private readonly bool _useErrorOut;
    private readonly List<IDisposable> _disposables = new(capacity: 32);

    public QueryCatConsoleLoggerProvider(bool useErrorOut = false)
    {
        _useErrorOut = useErrorOut;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        var dotIndex = categoryName.LastIndexOf('.');
        if (dotIndex > -1)
        {
            categoryName = categoryName.Substring(dotIndex + 1);
        }
        var logger = new QueryCatConsoleLogger(categoryName, _useErrorOut);
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
