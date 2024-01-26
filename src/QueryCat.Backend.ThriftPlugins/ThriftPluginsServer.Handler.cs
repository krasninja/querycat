using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;
using LogLevel = QueryCat.Plugins.Sdk.LogLevel;

namespace QueryCat.Backend.ThriftPlugins;

public partial class ThriftPluginsServer
{
    internal sealed class PluginContext
    {
        public string Name { get; set; } = string.Empty;

        public TProtocol? Protocol { get; set; }

        public Plugin.Client? Client { get; set; }

        public List<PluginContextFunction> Functions { get; } = new();

        public ObjectsStorage ObjectsStorage { get; } = new();

        public IntPtr? LibraryHandle { get; set; }

        public void Shutdown()
        {
            ObjectsStorage.Clean();
            if (Client != null)
            {
                AsyncUtils.RunSync(Client.ShutdownAsync);
            }
            if (LibraryHandle.HasValue && LibraryHandle.Value != IntPtr.Zero)
            {
                // For some reason it causes SIGSEGV (Address boundary error) on Linux.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    NativeLibrary.Free(LibraryHandle.Value);
                }
            }
        }
    }

    private sealed class Handler : PluginsManager.IAsync
    {
        private readonly ThriftPluginsServer _thriftPluginsServer;

        public Handler(ThriftPluginsServer thriftPluginsServer)
        {
            _thriftPluginsServer = thriftPluginsServer;
        }

        /// <inheritdoc />
        public async Task<RegistrationResult> RegisterPluginAsync(
            string auth_token,
            string callback_uri,
            PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            if (plugin_data == null)
            {
                return CreateEmptyRegistrationResult();
            }

            // Validate authentication token.
            _thriftPluginsServer._logger.LogTrace(
                "Pre-register plugin '{PluginName}' ({PluginVersion}) with token '{Token}' and callback URI '{CallbackUri}'.",
                plugin_data.Name,
                plugin_data.Version,
                auth_token,
                callback_uri);
            if (!_thriftPluginsServer.SkipTokenVerification && !_thriftPluginsServer.VerifyAuthToken(auth_token))
            {
                throw new QueryCatPluginException(ErrorType.INVALID_AUTH_TOKEN, "Invalid token.");
            }

            // Create plugin context, init and add it to a list.GetFileByContext
            var context = await CreateClientConnection(callback_uri, cancellationToken);
            if (!string.IsNullOrEmpty(plugin_data.Name))
            {
                context.Name = plugin_data.Name;
            }
            if (string.IsNullOrEmpty(context.Name))
            {
                context.Name = _thriftPluginsServer.GetPluginNameByAuthToken(auth_token);
            }
            if (plugin_data.Functions != null)
            {
                foreach (var function in plugin_data.Functions)
                {
                    context.Functions.Add(
                        new PluginContextFunction(function.Signature, function.Description));
                }
            }
            _thriftPluginsServer.RegisterPluginContext(context, auth_token);

            // Since we registered plugin we can release semaphore and notify loader.
            _thriftPluginsServer.ConfirmAuthToken(auth_token);
            _thriftPluginsServer._logger.LogDebug("Registered plugin '{PluginName}'.", context.Name);

            return CreateEmptyRegistrationResult();
        }

        private static RegistrationResult CreateEmptyRegistrationResult()
            => new(Application.GetVersion(), new List<int>());

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
                    ThriftPluginClient.PluginServerName),
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
            var argsForFunction = new FunctionCallArguments();
            foreach (var arg in args)
            {
                argsForFunction.Add(SdkConvert.Convert(arg));
            }
            var function = _thriftPluginsServer._executionThread
                .FunctionsManager.FindByName(function_name, argsForFunction.GetTypes());
            var result = _thriftPluginsServer._executionThread.FunctionsManager.CallFunction(
                function, _thriftPluginsServer._executionThread, argsForFunction);
            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task<VariantValue> RunQueryAsync(string query, Dictionary<string, VariantValue>? parameters,
            CancellationToken cancellationToken = default)
        {
            var @params = (parameters ?? new Dictionary<string, VariantValue>())
                .ToDictionary(k => k.Key, v => SdkConvert.Convert(v.Value));
            var result = _thriftPluginsServer._executionThread.Run(query, @params, cancellationToken);
            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task SetConfigValueAsync(string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            var internalValue = value != null ? SdkConvert.Convert(value) : Core.Types.VariantValue.Null;
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
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));

        public HandlerWithExceptionIntercept(PluginsManager.IAsync handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public Task<RegistrationResult> RegisterPluginAsync(string auth_token, string callback_uri, PluginData? plugin_data,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RegisterPluginAsync(auth_token, callback_uri, plugin_data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> RunQueryAsync(string query, Dictionary<string, VariantValue>? parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RunQueryAsync(query, parameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task SetConfigValueAsync(string key, VariantValue? value, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.SetConfigValueAsync(key, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<VariantValue> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.GetConfigValueAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task LogAsync(LogLevel level, string message, List<string>? arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.LogAsync(level, message, arguments, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }
    }
}
