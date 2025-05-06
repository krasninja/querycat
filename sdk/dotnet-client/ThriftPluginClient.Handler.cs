using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Sdk;
using Column = QueryCat.Plugins.Sdk.Column;
using CursorSeekOrigin = QueryCat.Plugins.Sdk.CursorSeekOrigin;
using DataType = QueryCat.Backend.Core.Types.DataType;
using KeyColumn = QueryCat.Plugins.Sdk.KeyColumn;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed partial class Handler : Plugin.IAsync
    {
        private readonly ThriftPluginClient _thriftPluginClient;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));

        public Handler(ThriftPluginClient thriftPluginClient)
        {
            _thriftPluginClient = thriftPluginClient;
        }

        /// <inheritdoc />
        public async Task<VariantValue> CallFunctionAsync(
            string function_name,
            List<VariantValue>? args,
            int object_handle,
            CancellationToken cancellationToken = default)
        {
            args ??= new List<VariantValue>();

            var func = _thriftPluginClient.FunctionsManager.FindByNameFirst(function_name);
            var frame = _thriftPluginClient._executionThread.Stack.CreateFrame();
            foreach (var arg in args)
            {
                frame.Push(SdkConvert.Convert(arg));
            }
            var result = await FunctionCaller.CallAsync(
                func.Delegate,
                _thriftPluginClient._executionThread,
                cancellationToken);
            frame.Dispose();
            var resultType = result.Type;
            if (resultType == DataType.Object)
            {
                if (result.AsObject is IRowsIterator rowsIterator)
                {
                    var index = _thriftPluginClient._objectsStorage.Add(rowsIterator);
                    _thriftPluginClient._logger.LogDebug("Added new iterator object '{Object}' with handle {Handle}.",
                        rowsIterator.ToString(), index);
                    return new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_ITERATOR, index, rowsIterator.ToString() ?? string.Empty),
                    };
                }
                else if (result.AsObject is IRowsInput rowsInput)
                {
                    rowsInput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(new List<Backend.Core.Data.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage);
                    var index =_thriftPluginClient._objectsStorage.Add(rowsInput);
                    _thriftPluginClient._logger.LogDebug("Added new input object '{Object}' with handle {Handle}.",
                        rowsInput.ToString(), index);
                    return new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_INPUT, index, rowsInput.ToString() ?? string.Empty),
                    };
                }
                else if (result.AsObject is IRowsOutput rowsOutput)
                {
                    rowsOutput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(new List<Backend.Core.Data.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage);
                    var index =_thriftPluginClient._objectsStorage.Add(rowsOutput);
                    _thriftPluginClient._logger.LogDebug("Added new output object '{Object}' with handle {Handle}.",
                        rowsOutput.ToString(), index);
                    return new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_OUTPUT, index, rowsOutput.ToString() ?? string.Empty),
                    };
                }
                else if (result.AsObject is IRowsFormatter rowsFormatter)
                {
                    var index =_thriftPluginClient._objectsStorage.Add(rowsFormatter);
                    _thriftPluginClient._logger.LogDebug("Added new formatter object '{Object}' with handle {Handle}.",
                        rowsFormatter.ToString(), index);
                    return new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_FORMATTER, index, rowsFormatter.ToString() ?? string.Empty),
                    };
                }
                throw new QueryCatPluginException(
                    ErrorType.INVALID_OBJECT,
                    string.Format(Resources.Errors.Object_CannotRegister, result.AsObject));
            }
            if (resultType == DataType.Blob)
            {
                var index = _thriftPluginClient._objectsStorage.Add(result.AsBlobUnsafe);
                _thriftPluginClient._logger.LogDebug("Added new blob object with handle {Handle}.", index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.BLOB, index, "BLOB"),
                };
            }

            return SdkConvert.Convert(result);
        }

        /// <inheritdoc />
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _thriftPluginClient._objectsStorage.Clean();
            _thriftPluginClient.SignalExit();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<Sdk.Column>> RowsSet_GetColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            var rowsSchema = _thriftPluginClient._objectsStorage.Get<IRowsSchema>(object_rows_set_handle);
            var columns = rowsSchema.Columns.Select(SdkConvert.Convert).ToList();
            return Task.FromResult(columns);
        }

        /// <inheritdoc />
        public async Task RowsSet_OpenAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                await rowsSource.OpenAsync(cancellationToken);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_CloseAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsSource>(object_handle, out var rowsSource)
                && rowsSource != null)
            {
                await rowsSource.CloseAsync(cancellationToken);
            }
            else
            {
                LogCannotFindObject(object_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_ResetAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                await rowsSource.ResetAsync(cancellationToken);
            }
            else if (_thriftPluginClient._objectsStorage.TryGet<IRowsIterator>(object_rows_set_handle, out var rowsIterator)
                && rowsIterator != null)
            {
                await rowsIterator.ResetAsync(cancellationToken);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public Task<int> RowsSet_PositionAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                return Task.FromResult(rowsSource.Position);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.FromResult(-1);
        }

        /// <inheritdoc />
        public Task<int> RowsSet_TotalRowsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                return Task.FromResult(rowsSource.TotalRows);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.FromResult(-1);
        }

        /// <inheritdoc />
        public Task RowsSet_SeekAsync(int object_rows_set_handle, int offset, CursorSeekOrigin origin,
            CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                rowsSource.Seek(offset, SdkConvert.Convert(origin));
                return Task.FromResult(rowsSource.Position);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_SetContextAsync(int object_rows_set_handle, ContextQueryInfo? context_query_info,
            CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
                && rowsSource != null)
            {
                if (context_query_info == null)
                {
                    rowsSource.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(Array.Empty<Backend.Core.Data.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage
                    );
                }
                else
                {
                    var columns = context_query_info.Columns ?? new List<QueryCat.Plugins.Sdk.Column>();
                    rowsSource.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(
                            columns.Select(SdkConvert.Convert).ToList(),
                            context_query_info.Limit)
                        {
                            Offset = context_query_info.Offset,
                        },
                        _thriftPluginClient._executionThread.ConfigStorage
                    );
                }
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<RowsList> RowsSet_GetRowsAsync(int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
        {
            // Handle IRowsInput.
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
                && rowsInput != null)
            {
                var values = new List<VariantValue>();
                var hasMore = true;
                for (var i = 0; i < count && (hasMore = await rowsInput.ReadNextAsync(cancellationToken)); i++)
                {
                    for (var colIndex = 0; colIndex < rowsInput.Columns.Length; colIndex++)
                    {
                        if (rowsInput.ReadValue(colIndex, out var value) == ErrorCode.OK)
                        {
                            values.Add(SdkConvert.Convert(value));
                        }
                        else
                        {
                            values.Add(SdkConvert.Convert(QueryCat.Backend.Core.Types.VariantValue.Null));
                        }
                    }
                }

                var result = new RowsList(values)
                {
                    HasMore = hasMore,
                };
                return result;
            }

            // Handle IRowsIterator.
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsIterator>(object_rows_set_handle, out var rowsIterator)
                && rowsIterator != null)
            {
                var values = new List<VariantValue>();
                var hasMore = true;
                for (var i = 0; i < count && (hasMore = await rowsIterator.MoveNextAsync(cancellationToken)); i++)
                {
                    foreach (var value in rowsIterator.Current.Values)
                    {
                        values.Add(SdkConvert.Convert(value));
                    }
                }
                var result = new RowsList(values)
                {
                    HasMore = hasMore,
                };
                return result;
            }

            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_Invalid);
        }

        /// <inheritdoc />
        public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
                && rowsInput != null)
            {
                var keys = rowsInput.UniqueKey.ToList();
                return Task.FromResult(keys);
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.FromResult(new List<string>());
        }

        /// <inheritdoc />
        public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
                && rowsInput != null)
            {
                if (rowsInput is IRowsInputKeys rowsInputKeys)
                {
                    var result = rowsInputKeys.GetKeyColumns()
                        .Select(c => new KeyColumn(
                            c.ColumnIndex,
                            c.IsRequired,
                            c.GetOperations().Select(o => o.ToString()).ToList())
                        );
                    return Task.FromResult(result.ToList());
                }
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.FromResult(new List<KeyColumn>());
        }

        /// <inheritdoc />
        public Task RowsSet_SetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            if (value != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
                && rowsInput != null)
            {
                if (rowsInput is IRowsInputKeys rowsInputKeys)
                {
                    rowsInputKeys.SetKeyColumnValue(column_index, SdkConvert.Convert(value),
                        Enum.Parse<Backend.Core.Types.VariantValue.Operation>(operation));
                }
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_UnsetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation,
            CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
                && rowsInput != null
                && rowsInput is IRowsInputKeys rowsInputKeys)
            {
                rowsInputKeys.UnsetKeyColumnValue(column_index, Enum.Parse<Backend.Core.Types.VariantValue.Operation>(operation));
            }
            else
            {
                LogCannotFindObject(object_rows_set_handle);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(int object_rows_set_handle, int column_index, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            if (value != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsInputUpdate>(object_rows_set_handle, out var rowsInputUpdate)
                && rowsInputUpdate != null)
            {
                var result = await rowsInputUpdate.UpdateValueAsync(column_index, SdkConvert.Convert(value), cancellationToken);
                return SdkConvert.Convert(result);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsInputUpdate)));
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(int object_rows_set_handle, List<VariantValue>? values,
            CancellationToken cancellationToken = default)
        {
            if (values != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsOutput>(object_rows_set_handle, out var rowsOutput)
                && rowsOutput != null)
            {
                var result = await rowsOutput.WriteValuesAsync(values.Select(SdkConvert.Convert).ToArray(), cancellationToken);
                return SdkConvert.Convert(result);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsOutput)));
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInputDelete>(object_rows_set_handle, out var rowsInputDelete)
                && rowsInputDelete != null)
            {
                var result = await rowsInputDelete.DeleteAsync(cancellationToken);
                return SdkConvert.Convert(result);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsInputDelete)));
        }

        /// <inheritdoc />
        public Task<int> RowsFormatter_OpenInputAsync(int object_rows_formatter_handle, int object_blob_handle, string? key, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsFormatter>(object_rows_formatter_handle, out var rowsFormatter)
                && rowsFormatter != null)
            {
                var remoteBlob = new RemoteBlobProxy(_thriftPluginClient.ThriftClient, object_blob_handle, _thriftPluginClient.Token);
                var rowsInput = rowsFormatter.OpenInput(remoteBlob, key);
                var index =_thriftPluginClient._objectsStorage.Add(rowsInput);
                _thriftPluginClient._logger.LogDebug("Added new input object '{Object}' with handle {Handle}.",
                    rowsInput.ToString(), index);
                return Task.FromResult(index);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsFormatter)));
        }

        /// <inheritdoc />
        public Task<int> RowsFormatter_OpenOutputAsync(int object_rows_formatter_handle, int object_blob_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsFormatter>(object_rows_formatter_handle, out var rowsFormatter)
                && rowsFormatter != null)
            {
                var remoteBlob = new RemoteBlobProxy(_thriftPluginClient.ThriftClient, object_blob_handle, _thriftPluginClient.Token);
                var rowsOutput = rowsFormatter.OpenOutput(remoteBlob);
                var index =_thriftPluginClient._objectsStorage.Add(rowsOutput);
                _thriftPluginClient._logger.LogDebug("Added new output object '{Object}' with handle {Handle}.",
                    rowsOutput.ToString(), index);
                return Task.FromResult(index);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsFormatter)));
        }

        /// <inheritdoc />
        public async Task<byte[]> Blob_ReadAsync(int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                await using var stream = blobData.GetStream();
                var arr = new byte[stream.Length];
                _ = await stream.ReadAsync(arr, 0, arr.Length, cancellationToken);
                return arr;
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }

        /// <inheritdoc />
        public async Task<long> Blob_WriteAsync(int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                await using var stream = blobData.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                return bytes.Length;
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }

        /// <inheritdoc />
        public Task<long> Blob_GetLengthAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                var length = blobData.Length;
                return Task.FromResult(length);
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }

        /// <inheritdoc />
        public Task<string> Blob_GetContentTypeAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
                && blobData != null)
            {
                return Task.FromResult(blobData.ContentType);
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }

        /// <inheritdoc />
        public Task<string> ServeAsync(CancellationToken cancellationToken = default)
        {
            var uri = _thriftPluginClient.StartNewServer();
            return Task.FromResult(uri.ToString());
        }

        [LoggerMessage(LogLevel.Warning, "Cannot find object with handle {ObjectHandle}.")]
        private partial void LogCannotFindObject(int objectHandle);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed partial class HandlerWithExceptionIntercept : Plugin.IAsync
    {
        private readonly Plugin.IAsync _handler;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));
        private readonly bool _traceCalls;

        public HandlerWithExceptionIntercept(Plugin.IAsync handler)
        {
#if DEBUG
            _traceCalls = true;
#endif
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<VariantValue> CallFunctionAsync(string function_name, List<VariantValue>? args, int object_handle,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(CallFunctionAsync));
            try
            {
                return await _handler.CallFunctionAsync(function_name, args, object_handle, cancellationToken);
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
        public async Task<List<Column>> RowsSet_GetColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetColumnsAsync));
            try
            {
                return await _handler.RowsSet_GetColumnsAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_OpenAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_OpenAsync));
            try
            {
                await _handler.RowsSet_OpenAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_CloseAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_CloseAsync));
            try
            {
                await _handler.RowsSet_CloseAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_ResetAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_ResetAsync));
            try
            {
                await _handler.RowsSet_ResetAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsSet_PositionAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_PositionAsync));
            try
            {
                return await _handler.RowsSet_PositionAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsSet_TotalRowsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_TotalRowsAsync));
            try
            {
                return await _handler.RowsSet_TotalRowsAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SeekAsync(int object_rows_set_handle, int offset, CursorSeekOrigin origin,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SeekAsync));
            try
            {
                await _handler.RowsSet_SeekAsync(object_rows_set_handle, offset, origin, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SetContextAsync(int object_rows_set_handle, ContextQueryInfo? context_query_info,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SetContextAsync));
            try
            {
                await _handler.RowsSet_SetContextAsync(object_rows_set_handle, context_query_info, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<RowsList> RowsSet_GetRowsAsync(int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetRowsAsync));
            try
            {
                return await _handler.RowsSet_GetRowsAsync(object_rows_set_handle, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetUniqueKeyAsync));
            try
            {
                return await _handler.RowsSet_GetUniqueKeyAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex,objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_GetKeyColumnsAsync));
            try
            {
                return await _handler.RowsSet_GetKeyColumnsAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_SetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_SetKeyColumnValueAsync));
            try
            {
                await _handler.RowsSet_SetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task RowsSet_UnsetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_UnsetKeyColumnValueAsync));
            try
            {
                await _handler.RowsSet_UnsetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(int object_rows_set_handle, int column_index, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_UpdateValueAsync));
            try
            {
                return await _handler.RowsSet_UpdateValueAsync(object_rows_set_handle, column_index, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(int object_rows_set_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_WriteValuesAsync));
            try
            {
                return await _handler.RowsSet_WriteValuesAsync(object_rows_set_handle, values, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsSet_DeleteRowAsync));
            try
            {
                return await _handler.RowsSet_DeleteRowAsync(object_rows_set_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_set_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsFormatter_OpenInputAsync(int object_rows_formatter_handle, int object_blob_handle, string? key,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsFormatter_OpenInputAsync));
            try
            {
                return await _handler.RowsFormatter_OpenInputAsync(object_rows_formatter_handle, object_blob_handle, key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_formatter_handle);
            }
        }

        /// <inheritdoc />
        public async Task<int> RowsFormatter_OpenOutputAsync(int object_rows_formatter_handle, int object_blob_handle,
            CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(RowsFormatter_OpenOutputAsync));
            try
            {
                return await _handler.RowsFormatter_OpenOutputAsync(object_rows_formatter_handle, object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_rows_formatter_handle);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> Blob_ReadAsync(int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_ReadAsync));
            try
            {
                return await _handler.Blob_ReadAsync(object_blob_handle, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_WriteAsync(int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_WriteAsync));
            try
            {
                return await _handler.Blob_WriteAsync(object_blob_handle, bytes, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<long> Blob_GetLengthAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_GetLengthAsync));
            try
            {
                return await _handler.Blob_GetLengthAsync(object_blob_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_blob_handle);
            }
        }

        /// <inheritdoc />
        public async Task<string> Blob_GetContentTypeAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        {
            LogCallMethod(nameof(Blob_GetContentTypeAsync));
            try
            {
                return await _handler.Blob_GetContentTypeAsync(object_blob_handle, cancellationToken);
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
