using Microsoft.Extensions.Logging;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using LogLevel = QueryCat.Plugins.Sdk.LogLevel;

namespace QueryCat.Backend.ThriftPlugins;

public partial class ThriftPluginsServer
{
    private sealed class Handler : PluginsManager.IAsync
    {
        private readonly ThriftPluginsServer _thriftPluginsServer;

        public Handler(ThriftPluginsServer thriftPluginsServer)
        {
            _thriftPluginsServer = thriftPluginsServer;
        }

        /// <inheritdoc />
        public async Task<RegistrationResult> RegisterPluginAsync(
            string registration_token,
            string callback_uri,
            PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            if (plugin_data == null)
            {
                return new RegistrationResult
                {
                    Version = Application.GetVersion(),
                    Token = -1
                };
            }

            // Validate authentication token.
            _thriftPluginsServer._logger.LogTrace(
                "Pre-register plugin '{PluginName}' ({PluginVersion}) with token '{Token}' and callback URI '{CallbackUri}'.",
                plugin_data.Name,
                plugin_data.Version,
                registration_token,
                callback_uri);
            if (!_thriftPluginsServer.SkipTokenVerification && !_thriftPluginsServer.VerifyRegistrationToken(registration_token))
            {
                if (_thriftPluginsServer._logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                {
                    _thriftPluginsServer._logger.LogDebug("Available tokens:\n" + _thriftPluginsServer.DumpRegistrationTokens());
                }
                throw new QueryCatPluginException(ErrorType.INVALID_REGISTRATION_TOKEN, Resources.Errors.InvalidToken);
            }

            // Create plugin context, init and add it to a list. "callback_uri" is the URI
            // provided by the client to connect to it.
            var context = CreateClientConnection(callback_uri);
            if (!string.IsNullOrEmpty(plugin_data.Name))
            {
                context.PluginName = plugin_data.Name;
            }
            if (string.IsNullOrEmpty(context.PluginName))
            {
                context.PluginName = _thriftPluginsServer.GetPluginNameByRegistrationToken(registration_token);
            }
            if (plugin_data.Functions != null)
            {
                foreach (var function in plugin_data.Functions)
                {
                    context.Functions.Add(
                        new PluginContextFunction(function.Signature, function.Description, function.IsSafe, function.IsAggregate));
                }
            }
            var token = _thriftPluginsServer.GenerateToken();
            _thriftPluginsServer.RegisterPluginContext(context, registration_token, token);

            // Since we registered plugin we can release semaphore and notify loader.
            _thriftPluginsServer.ConfirmRegistrationToken(registration_token);
            _thriftPluginsServer._logger.LogDebug("Registered plugin '{PluginName}'.", context.PluginName);

            return new RegistrationResult(
                token,
                Application.GetVersion());
        }

        private ThriftPluginContext CreateClientConnection(string callbackUri)
        {
            var context = new ThriftPluginContext(callbackUri, maxConnections: _thriftPluginsServer._maxConnectionsToClient);
            _thriftPluginsServer._logger.LogTrace("Create plugin context, URI '{CallbackUri}'.", callbackUri);
            return context;
        }

        /// <inheritdoc />
        public async Task<VariantValue> CallFunctionAsync(
            long token,
            string function_name,
            List<VariantValue>? args,
            int object_handle,
            CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            args ??= new List<VariantValue>();
            var argsForFunction = new FunctionCallArguments();
            foreach (var arg in args)
            {
                argsForFunction.Add(SdkConvert.Convert(arg));
            }
            var function = _thriftPluginsServer._executionThread
                .FunctionsManager.FindByNameFirst(function_name, argsForFunction.GetTypes());
            var result = await _thriftPluginsServer._executionThread.FunctionsManager.CallFunctionAsync(
                function, _thriftPluginsServer._executionThread, argsForFunction, cancellationToken);
            return SdkConvert.Convert(result);
        }

        /// <inheritdoc />
        public async Task<VariantValue> RunQueryAsync(long token,
            string query,
            Dictionary<string, VariantValue>? parameters,
            CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var @params = (parameters ?? new Dictionary<string, VariantValue>())
                .ToDictionary(k => k.Key, v => SdkConvert.Convert(v.Value));
            var result = await _thriftPluginsServer._executionThread.RunAsync(query, @params, cancellationToken);
            return SdkConvert.Convert(result);
        }

        /// <inheritdoc />
        public async Task SetConfigValueAsync(long token, string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var internalValue = value != null ? SdkConvert.Convert(value) : Core.Types.VariantValue.Null;
            await _thriftPluginsServer._inputConfigStorage.SetAsync(key, internalValue, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VariantValue> GetConfigValueAsync(long token, string key, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var value = await _thriftPluginsServer._inputConfigStorage.GetAsync(key, cancellationToken);
            return SdkConvert.Convert(value);
        }

        /// <inheritdoc />
        public Task<VariantValue> GetVariableAsync(long token, string name, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            if (_thriftPluginsServer._executionThread.TryGetVariable(name, out var value))
            {
                return Task.FromResult(SdkConvert.Convert(value));
            }
            return Task.FromResult(SdkConvert.Convert(Core.Types.VariantValue.Null));
        }

        /// <inheritdoc />
        public Task<VariantValue> SetVariableAsync(long token, string name, VariantValue? value, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            if (value == null)
            {
                return Task.FromResult(SdkConvert.Convert(Core.Types.VariantValue.Null));
            }
            _thriftPluginsServer._executionThread.TopScope.Variables[name] = SdkConvert.Convert(value);
            return Task.FromResult(value);
        }

        /// <inheritdoc />
        public Task LogAsync(long token, LogLevel level, string message, List<string>? arguments,
            CancellationToken cancellationToken = default)
        {
            var context = _thriftPluginsServer.GetPluginContextByToken(token);
            var logLevel = level switch
            {
                LogLevel.TRACE => Microsoft.Extensions.Logging.LogLevel.Trace,
                LogLevel.DEBUG => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogLevel.INFORMATION => Microsoft.Extensions.Logging.LogLevel.Information,
                LogLevel.WARNING => Microsoft.Extensions.Logging.LogLevel.Warning,
                LogLevel.ERROR => Microsoft.Extensions.Logging.LogLevel.Error,
                LogLevel.CRITICAL => Microsoft.Extensions.Logging.LogLevel.Critical,
                LogLevel.NONE => Microsoft.Extensions.Logging.LogLevel.None,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
            };

            message = $"[{context.PluginName}] {message}";
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
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(HandlerWithExceptionIntercept));

        public HandlerWithExceptionIntercept(PluginsManager.IAsync handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public Task<RegistrationResult> RegisterPluginAsync(string registration_token, string callback_uri, PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RegisterPluginAsync(registration_token, callback_uri, plugin_data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> CallFunctionAsync(long token, string function_name, List<VariantValue>? args, int object_handle,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.CallFunctionAsync(token, function_name, args, object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> RunQueryAsync(long token, string query, Dictionary<string, VariantValue>? parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RunQueryAsync(token, query, parameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task SetConfigValueAsync(long token, string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.SetConfigValueAsync(token, key, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> GetConfigValueAsync(long token, string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.GetConfigValueAsync(token, key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> GetVariableAsync(long token, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.GetVariableAsync(token, name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> SetVariableAsync(long token, string name, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.SetVariableAsync(token, name, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task LogAsync(long token, LogLevel level, string message, List<string>? arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.LogAsync(token, level, message, arguments, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }
    }
}
