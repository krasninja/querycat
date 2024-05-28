using System;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Plugins.Client.Logging;

internal sealed class ThriftClientLogger : ILogger
{
    private readonly string _name;
    private readonly ThriftPluginClient _client;

    public ThriftClientLogger(string name, ThriftPluginClient client)
    {
        _name = name;
        _client = client;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    private static global::QueryCat.Plugins.Sdk.LogLevel GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Sdk.LogLevel.TRACE,
            LogLevel.Debug => Sdk.LogLevel.DEBUG,
            LogLevel.Information => Sdk.LogLevel.INFORMATION,
            LogLevel.Warning => Sdk.LogLevel.WARNING,
            LogLevel.Error => Sdk.LogLevel.ERROR,
            LogLevel.Critical => Sdk.LogLevel.CRITICAL,
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
        if (!_client.IsActive)
        {
            return;
        }

        var message = string.Concat(_name, ": ",  formatter.Invoke(state, exception));
        AsyncUtils.RunSync(() => _client.LogAsync(
            level: GetLogLevelString(logLevel),
            message: message)
        );
    }
}
