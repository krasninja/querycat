using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Client.Logging;

internal sealed class ThriftClientLoggerProvider : ILoggerProvider
{
    private readonly ThriftPluginClient _client;

    public ThriftClientLoggerProvider(ThriftPluginClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        var dotIndex = categoryName.LastIndexOf('.');
        if (dotIndex > -1)
        {
            categoryName = categoryName.Substring(dotIndex + 1);
        }

        var logger = new ThriftClientLogger(categoryName, _client);
        return logger;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
