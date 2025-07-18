using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using CompletionResult = QueryCat.Plugins.Sdk.CompletionResult;
using LogLevel = QueryCat.Plugins.Sdk.LogLevel;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Backend.ThriftPlugins;

public partial class ThriftPluginsServer
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Generated by Thrift.")]
    private sealed class Handler : PluginsManager.IAsync
    {
        private readonly ThriftPluginsServer _thriftPluginsServer;
        private readonly Dictionary<int, IExecutionScope> _scopeIdToScope = new();
        private readonly Dictionary<IExecutionScope, int> _scopeToScopeId = new();
        private int _executionScopeId;

        public Handler(ThriftPluginsServer thriftPluginsServer)
        {
            _thriftPluginsServer = thriftPluginsServer;
        }

        /// <inheritdoc />
        public Task<RegistrationResult> RegisterPluginAsync(
            string registration_token,
            string callback_uri,
            PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            if (plugin_data == null)
            {
                return Task.FromResult(new RegistrationResult
                {
                    Version = Application.GetVersion(),
                    Token = -1,
                    MinLogLevel = SdkConvert.Convert(GetCurrentLogLevel()),
                });
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
                        new PluginContextFunction(
                            function.Signature,
                            function.Description,
                            function.IsSafe,
                            function.IsAggregate,
                            (function.FormatterIds ?? []).ToArray()
                        )
                    );
                }
            }
            var token = _thriftPluginsServer.GenerateToken();
            _thriftPluginsServer.RegisterPluginContext(context, registration_token, token);

            // Since we registered plugin we can release semaphore and notify loader.
            _thriftPluginsServer.ConfirmRegistrationToken(registration_token);
            _thriftPluginsServer._logger.LogDebug("Registered plugin '{PluginName}'.", context.PluginName);

            return Task.FromResult(new RegistrationResult
            {
                Token = token,
                Version = Application.GetVersion(),
                MinLogLevel = SdkConvert.Convert(GetCurrentLogLevel()),
            });
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
            await _thriftPluginsServer._configStorage.SetAsync(key, internalValue, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VariantValue> GetConfigValueAsync(long token, string key, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var value = await _thriftPluginsServer._configStorage.GetAsync(key, cancellationToken);
            return SdkConvert.Convert(value);
        }

        /// <inheritdoc />
        public Task<VariantValue> GetVariableAsync(long token, string name, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            if (_thriftPluginsServer._executionThread.TopScope.TryGetVariable(name, out var value))
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
        public Task<List<ScopeVariable>> GetVariablesAsync(long token, int scope_id, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            if (_scopeIdToScope.TryGetValue(scope_id, out var scope))
            {
                var result = scope.Variables.Select(v => new ScopeVariable(v.Key, SdkConvert.Convert(v.Value))).ToList();
                return Task.FromResult(result);
            }
            return Task.FromResult(new List<ScopeVariable>());
        }

        private int AddScope(IExecutionScope scope)
        {
            if (_scopeToScopeId.TryGetValue(scope, out var scopeId))
            {
                return scopeId;
            }
            scopeId = Interlocked.Increment(ref _executionScopeId);
            _scopeIdToScope[scopeId] = scope;
            _scopeToScopeId[scope] = scopeId;
            return scopeId;
        }

        private int RemoveScope(IExecutionScope scope)
        {
            if (_scopeToScopeId.Remove(scope, out var scopeId))
            {
                _scopeIdToScope.Remove(scopeId);
                return scopeId;
            }
            return ThriftPluginExecutionScope.NoScopeId;
        }

        private int GetScopeParentId(IExecutionScope scope)
        {
            if (scope.Parent != null && _scopeToScopeId.TryGetValue(scope.Parent, out var parentScopeId))
            {
                return parentScopeId;
            }
            return ThriftPluginExecutionScope.NoScopeId;
        }

        /// <inheritdoc />
        public Task<ExecutionScope> PushScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var scope = _thriftPluginsServer._executionThread.PushScope();
            var scopeId = AddScope(scope);
            return Task.FromResult(new ExecutionScope(scopeId, GetScopeParentId(scope)));
        }

        /// <inheritdoc />
        public Task<ExecutionScope> PopScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var scope = _thriftPluginsServer._executionThread.PopScope();
            if (scope != null)
            {
                var scopeId = AddScope(scope); // Do not remove it because it can be used in closure code.
                return Task.FromResult(new ExecutionScope(scopeId, GetScopeParentId(scope)));
            }
            return Task.FromResult(new ExecutionScope(ThriftPluginExecutionScope.NoScopeId, ThriftPluginExecutionScope.NoScopeId));
        }

        /// <inheritdoc />
        public Task<ExecutionScope> PeekTopScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var topScope = _thriftPluginsServer._executionThread.TopScope;
            var scopeId = AddScope(topScope);
            return Task.FromResult(new ExecutionScope(scopeId, GetScopeParentId(topScope)));
        }

        /// <inheritdoc />
        public async Task<List<CompletionResult>> GetCompletionsAsync(long token, string text, int position, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var completions = await _thriftPluginsServer._executionThread.GetCompletionsAsync(text, position, null, cancellationToken)
                .ToListAsync(cancellationToken);
            return completions.Select(SdkConvert.Convert).ToList();
        }

        /// <inheritdoc />
        public async Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var pluginContext = _thriftPluginsServer.GetPluginContextByToken(token);
            if (pluginContext.ObjectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                await using var stream = blobData.GetStream();
                var buffer = new byte[count];
                if (offset >= stream.Length)
                {
                    return [];
                }
                if (offset > 0)
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                }
                var readBytes = await stream.ReadAsync(buffer, 0, count, cancellationToken);
                if (readBytes != buffer.Length)
                {
                    buffer = buffer.AsSpan(0, readBytes).ToArray();
                }
                return buffer;
            }
            throw new QueryCatException(Resources.Errors.InvalidBlobHandle);
        }

        /// <inheritdoc />
        public async Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var pluginContext = _thriftPluginsServer.GetPluginContextByToken(token);
            if (pluginContext.ObjectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                await using var stream = blobData.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            throw new QueryCatException(Resources.Errors.InvalidBlobHandle);
        }

        /// <inheritdoc />
        public Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var pluginContext = _thriftPluginsServer.GetPluginContextByToken(token);
            if (pluginContext.ObjectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                return Task.FromResult(blobData.Length);
            }
            throw new QueryCatException(Resources.Errors.InvalidBlobHandle);
        }

        /// <inheritdoc />
        public Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var pluginContext = _thriftPluginsServer.GetPluginContextByToken(token);
            if (pluginContext.ObjectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                return Task.FromResult(blobData.ContentType);
            }
            throw new QueryCatException(Resources.Errors.InvalidBlobHandle);
        }

        /// <inheritdoc />
        public Task LogAsync(long token, LogLevel level, string message, List<string>? arguments,
            CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);

            var context = _thriftPluginsServer.GetPluginContextByToken(token);
            var logLevel = SdkConvert.Convert(level);

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

        /// <inheritdoc />
        public Task<Statistic> GetStatisticAsync(long token, CancellationToken cancellationToken = default)
        {
            _thriftPluginsServer.VerifyToken(token);
            var statistic = SdkConvert.Convert(_thriftPluginsServer._executionThread.Statistic);
            return Task.FromResult(statistic);
        }

        private Microsoft.Extensions.Logging.LogLevel GetCurrentLogLevel()
        {
            foreach (var logLevel in Enum.GetValues<Microsoft.Extensions.Logging.LogLevel>())
            {
                if (_thriftPluginsServer._logger.IsEnabled(logLevel))
                {
                    return logLevel;
                }
            }

            return Microsoft.Extensions.Logging.LogLevel.None;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Generated by Thrift.")]
    private sealed class HandlerWithExceptionIntercept : PluginsManager.IAsync
    {
        private readonly PluginsManager.IAsync _handler;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(HandlerWithExceptionIntercept));

        public HandlerWithExceptionIntercept(PluginsManager.IAsync handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<RegistrationResult> RegisterPluginAsync(string registration_token, string callback_uri, PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.RegisterPluginAsync(registration_token, callback_uri, plugin_data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<VariantValue> CallFunctionAsync(long token, string function_name, List<VariantValue>? args, int object_handle,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.CallFunctionAsync(token, function_name, args, object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<VariantValue> RunQueryAsync(long token, string query, Dictionary<string, VariantValue>? parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.RunQueryAsync(token, query, parameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task SetConfigValueAsync(long token, string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                await _handler.SetConfigValueAsync(token, key, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<VariantValue> GetConfigValueAsync(long token, string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.GetConfigValueAsync(token, key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<VariantValue> GetVariableAsync(long token, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.GetVariableAsync(token, name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<VariantValue> SetVariableAsync(long token, string name, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.SetVariableAsync(token, name, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<ScopeVariable>> GetVariablesAsync(long token, int scope_id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.GetVariablesAsync(token, scope_id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<ExecutionScope> PushScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.PushScopeAsync(token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<ExecutionScope> PopScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.PopScopeAsync(token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<ExecutionScope> PeekTopScopeAsync(long token, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.PeekTopScopeAsync(token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<CompletionResult>> GetCompletionsAsync(long token, string text, int position, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.GetCompletionsAsync(token, text, position, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.Blob_ReadAsync(token, object_blob_handle, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.Blob_WriteAsync(token, object_blob_handle, bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.Blob_GetLengthAsync(token, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _handler.Blob_GetContentTypeAsync(token, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task LogAsync(long token, LogLevel level, string message, List<string>? arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                await _handler.LogAsync(token, level, message, arguments, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<Statistic> GetStatisticAsync(long token, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.GetStatisticAsync(token, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }
    }
}
