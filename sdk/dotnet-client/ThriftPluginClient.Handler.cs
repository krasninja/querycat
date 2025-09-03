using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Plugins.Sdk;
using Column = QueryCat.Plugins.Sdk.Column;
using CursorSeekOrigin = QueryCat.Plugins.Sdk.CursorSeekOrigin;
using KeyColumn = QueryCat.Plugins.Sdk.KeyColumn;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using QuestionRequest = QueryCat.Plugins.Sdk.QuestionRequest;
using QuestionResponse = QueryCat.Plugins.Sdk.QuestionResponse;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed partial class Handler : QueryCatIOHandler, Plugin.IAsync
    {
        private readonly ThriftPluginClient _thriftPluginClient;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));

        public Handler(ThriftPluginClient thriftPluginClient)
            : base(thriftPluginClient._executionThread, thriftPluginClient._objectsStorage)
        {
            _thriftPluginClient = thriftPluginClient;
        }

        /// <inheritdoc />
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            await BeforeCallAsync(0, nameof(ShutdownAsync), cancellationToken);
            _thriftPluginClient._objectsStorage.Clean();
            _thriftPluginClient.SignalExit();
        }

        /// <inheritdoc />
        public async Task<string> ServeAsync(CancellationToken cancellationToken = default)
        {
            await BeforeCallAsync(0, nameof(ServeAsync), cancellationToken);
            var uri = _thriftPluginClient.StartNewServer();
            return uri.ToString();
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed partial class HandlerWithExceptionIntercept : Plugin.IAsync
    {
        private readonly Plugin.IAsync _handler;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private readonly bool _traceCalls;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        public HandlerWithExceptionIntercept(Plugin.IAsync handler)
        {
#if DEBUG
            _traceCalls = true;
#endif
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<VariantValue> CallFunctionAsync(long token, string function_name, List<VariantValue>? args, int object_handle,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(CallFunctionAsync));
            try
            {
                return await _handler.CallFunctionAsync(token, function_name, args, object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(ShutdownAsync));
            try
            {
                await _handler.ShutdownAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<Column>> RowsSet_GetColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetColumnsAsync));
            try
            {
                return await _handler.RowsSet_GetColumnsAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_OpenAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_OpenAsync));
            try
            {
                await _handler.RowsSet_OpenAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_CloseAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_CloseAsync));
            try
            {
                await _handler.RowsSet_CloseAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_ResetAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_ResetAsync));
            try
            {
                await _handler.RowsSet_ResetAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsSet_PositionAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_PositionAsync));
            try
            {
                return await _handler.RowsSet_PositionAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsSet_TotalRowsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_TotalRowsAsync));
            try
            {
                return await _handler.RowsSet_TotalRowsAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SeekAsync(long token, int object_rows_set_handle, int offset, CursorSeekOrigin origin,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SeekAsync));
            try
            {
                await _handler.RowsSet_SeekAsync(token, object_rows_set_handle, offset, origin, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SetContextAsync(long token, int object_rows_set_handle, ContextQueryInfo? context_query_info,
            ContextInfo? context_info, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SetContextAsync));
            try
            {
                await _handler.RowsSet_SetContextAsync(token, object_rows_set_handle, context_query_info, context_info, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<RowsList> RowsSet_GetRowsAsync(long token, int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetRowsAsync));
            try
            {
                return await _handler.RowsSet_GetRowsAsync(token, object_rows_set_handle, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<List<string>> RowsSet_GetUniqueKeyAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetUniqueKeyAsync));
            try
            {
                return await _handler.RowsSet_GetUniqueKeyAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex,objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetKeyColumnsAsync));
            try
            {
                return await _handler.RowsSet_GetKeyColumnsAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SetKeyColumnValueAsync));
            try
            {
                await _handler.RowsSet_SetKeyColumnValueAsync(token, object_rows_set_handle, column_index, operation, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_UnsetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_UnsetKeyColumnValueAsync));
            try
            {
                await _handler.RowsSet_UnsetKeyColumnValueAsync(token, object_rows_set_handle, column_index, operation, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(long token, int object_rows_set_handle, int column_index, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_UpdateValueAsync));
            try
            {
                return await _handler.RowsSet_UpdateValueAsync(token, object_rows_set_handle, column_index, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(long token, int object_rows_set_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_WriteValuesAsync));
            try
            {
                return await _handler.RowsSet_WriteValuesAsync(token, object_rows_set_handle, values, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_DeleteRowAsync));
            try
            {
                return await _handler.RowsSet_DeleteRowAsync(token, object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<ModelDescription> RowsSet_GetDescriptionAsync(long token, int object_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsFormatter_OpenInputAsync));
            try
            {
                return await _handler.RowsSet_GetDescriptionAsync(token, object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsFormatter_OpenInputAsync(long token, int object_rows_formatter_handle, int object_blob_handle, string? key,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsFormatter_OpenInputAsync));
            try
            {
                return await _handler.RowsFormatter_OpenInputAsync(token, object_rows_formatter_handle, object_blob_handle, key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_formatter_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsFormatter_OpenOutputAsync(long token, int object_rows_formatter_handle, int object_blob_handle,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsFormatter_OpenOutputAsync));
            try
            {
                return await _handler.RowsFormatter_OpenOutputAsync(token, object_rows_formatter_handle, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_formatter_handle);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_ReadAsync));
            try
            {
                return await _handler.Blob_ReadAsync(token, object_blob_handle, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_WriteAsync));
            try
            {
                return await _handler.Blob_WriteAsync(token, object_blob_handle, bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_GetLengthAsync));
            try
            {
                return await _handler.Blob_GetLengthAsync(token, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_GetContentTypeAsync));
            try
            {
                return await _handler.Blob_GetContentTypeAsync(token, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<string> Blob_GetNameAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_GetNameAsync));
            try
            {
                return await _handler.Blob_GetNameAsync(token, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<string> ServeAsync(CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(ServeAsync));
            try
            {
                return await _handler.ServeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public async Task<QuestionResponse> AnswerAgent_AskAsync(long token, int object_answer_agent_handle, QuestionRequest? request,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(AnswerAgent_AskAsync));
            try
            {
                return await _handler.AnswerAgent_AskAsync(token, object_answer_agent_handle, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        private void LogCallMethod(string methodName)
        {
            if (_traceCalls && _logger.IsEnabled(LogLevel.Trace))
            {
                LogCallMethodInternal(methodName);
            }
        }

        [LoggerMessage(LogLevel.Trace, "Call remote method {MethodName}.")]
        private partial void LogCallMethodInternal(string methodName);
    }
}
