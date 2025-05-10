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

        var message = string.Concat(_name, ": ", formatter.Invoke(state, exception));
        AsyncUtils.RunSync(ct => _client.LogAsync(
            level: SdkConvert.Convert(logLevel),
            message: message,
            cancellationToken: ct)
        );
    }
}
