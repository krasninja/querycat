using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions.Functions;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using LogLevel = QueryCat.Plugins.Sdk.LogLevel;

namespace QueryCat.Backend.ThriftPlugins;

public partial class ThriftPluginsServer
{
    internal class PluginContext
    {
        public string Name { get; set; } = string.Empty;

        public TProtocol? Protocol { get; set; }

        public Plugin.Client? Client { get; set; }

        public HashSet<string> Functions { get; } = new();

        public ObjectsStorage ObjectsStorage { get; } = new();
    }

    private sealed class Handler : PluginsManager.IAsync
    {
        private readonly ThriftPluginsServer _thriftPluginsServer;

        public Handler(ThriftPluginsServer thriftPluginsServer)
        {
            _thriftPluginsServer = thriftPluginsServer;
        }

        /// <inheritdoc />
        public async Task RegisterPluginAsync(
            string auth_token,
            string callback_uri,
            PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            if (plugin_data == null)
            {
                return;
            }

            _thriftPluginsServer._logger.LogTrace(
                "Pre-register plugin '{PluginName}' with token '{Token}' and callback URI '{CallbackUri}'.",
                plugin_data.Version, auth_token, callback_uri);
            SemaphoreSlim? semaphoreSlim = null;
            if (!_thriftPluginsServer.SkipTokenVerification &&
                (string.IsNullOrEmpty(auth_token)
                    || !_thriftPluginsServer._authTokens.TryGetValue(auth_token, out semaphoreSlim)))
            {
                throw new QueryCatPluginException(ErrorType.INVALID_AUTH_TOKEN, "Invalid token.");
            }

            var context = await CreateClientConnection(callback_uri, cancellationToken);

            if (plugin_data.Functions != null)
            {
                foreach (var function in plugin_data.Functions)
                {
                    context.Functions.Add(function);
                }
            }

            _thriftPluginsServer._plugins.Add(context);
            semaphoreSlim?.Release();

            _thriftPluginsServer._logger.LogDebug("Registered plugin '{PluginName}'.", plugin_data.Name);
        }

        private async Task<PluginContext> CreateClientConnection(
            string callbackUri,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri(callbackUri);
            var context = new PluginContext
            {
                Protocol = new TMultiplexedProtocol(
                    new TBinaryProtocol(
                        new TFramedTransport(
                            new TNamedPipeTransport(uri.Segments[1], new TConfiguration()))
                        ),
                    ThriftPluginClient.PluginServerName)
            };
            context.Client = new Plugins.Sdk.Plugin.Client(context.Protocol);
            await context.Client.OpenTransportAsync(cancellationToken);
            _thriftPluginsServer._logger.LogTrace("Connected to plugin callback URI '{CallbackUri}'.", callbackUri);
            return context;
        }

        /// <inheritdoc />
        public Task<VariantValue> CallFunctionAsync(
            string function_name,
            List<VariantValue>? args,
            int object_handle,
            CancellationToken cancellationToken = default)
        {
            args ??= new List<VariantValue>();
            var argsForFunction = new FunctionArguments();
            foreach (var arg in args)
            {
                argsForFunction.Add(SdkConvert.Convert(arg));
            }
            var result = _thriftPluginsServer._executionThread.FunctionsManager.CallFunction(function_name,
                argsForFunction);
            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task<VariantValue> RunQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            var result = _thriftPluginsServer._executionThread.Run(query);
            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task SetConfigValueAsync(string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            var internalValue = value != null ? SdkConvert.Convert(value) : Types.VariantValue.Null;
            _thriftPluginsServer._inputConfigStorage.Set(key, internalValue);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<VariantValue> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
        {
            var value = _thriftPluginsServer._inputConfigStorage.Get(key);
            return Task.FromResult(SdkConvert.Convert(value));
        }

        /// <inheritdoc />
        public Task LogAsync(LogLevel level, string message, List<string>? arguments, CancellationToken cancellationToken = default)
        {
            var logLevel = level switch
            {
                LogLevel.TRACE => Microsoft.Extensions.Logging.LogLevel.Trace,
                LogLevel.DEBUG => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.INFORMATION => Microsoft.Extensions.Logging.LogLevel.Information,
                LogLevel.WARNING => Microsoft.Extensions.Logging.LogLevel.Warning,
                LogLevel.CRITICAL => Microsoft.Extensions.Logging.LogLevel.Critical,
                LogLevel.NONE => Microsoft.Extensions.Logging.LogLevel.None,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
            };
            if (arguments != null && arguments.Count > 0)
            {
                _thriftPluginsServer._logger.Log(logLevel, message, args: arguments.Cast<object>().ToArray());
            }
            else
            {
                _thriftPluginsServer._logger.Log(logLevel, message);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerWithExceptionIntercept : PluginsManager.IAsync
    {
        private readonly PluginsManager.IAsync _handler;

        public HandlerWithExceptionIntercept(PluginsManager.IAsync handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public Task RegisterPluginAsync(string auth_token, string callback_uri, PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RegisterPluginAsync(auth_token, callback_uri, plugin_data, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> CallFunctionAsync(string function_name, List<VariantValue>? args, int object_handle,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.CallFunctionAsync(function_name, args, object_handle, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> RunQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RunQueryAsync(query, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message);
            }
        }

        /// <inheritdoc />
        public Task SetConfigValueAsync(string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.SetConfigValueAsync(key, value, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.GetConfigValueAsync(key, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message);
            }
        }

        /// <inheritdoc />
        public Task LogAsync(LogLevel level, string message, List<string>? arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.LogAsync(level, message, arguments, cancellationToken);
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message);
            }
        }
    }
}
