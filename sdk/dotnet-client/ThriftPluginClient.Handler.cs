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
using DataType = QueryCat.Backend.Core.Types.DataType;
using KeyColumn = QueryCat.Plugins.Sdk.KeyColumn;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed class Handler : Plugin.IAsync
    {
        private readonly ThriftPluginClient _thriftPluginClient;

        public Handler(ThriftPluginClient thriftPluginClient)
        {
            _thriftPluginClient = thriftPluginClient;
        }

        /// <inheritdoc />
        public Task<VariantValue> CallFunctionAsync(
            string function_name,
            List<VariantValue>? args,
            int object_handle,
            CancellationToken cancellationToken = default)
        {
            args ??= new List<VariantValue>();

            var func = _thriftPluginClient.FunctionsManager.FindByName(function_name);
            var callInfo = new FunctionCallInfo(_thriftPluginClient._executionThread, func.Name);
            foreach (var arg in args)
            {
                callInfo.Push(SdkConvert.Convert(arg));
            }
            var result = func.Delegate.Invoke(callInfo);
            var resultType = result.Type;
            if (resultType == DataType.Object)
            {
                if (result.AsObject is IRowsIterator rowsIterator)
                {
                    var index = _thriftPluginClient._objectsStorage.Add(rowsIterator);
                    _thriftPluginClient._logger.LogDebug("Added new iterator object '{Object}' with handle {Handle}.",
                        rowsIterator.ToString(), index);
                    return Task.FromResult(new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_ITERATOR, index, rowsIterator.ToString() ?? string.Empty),
                    });
                }
                else if (result.AsObject is IRowsInput rowsInput)
                {
                    rowsInput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(new List<Backend.Core.Data.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage);
                    var index =_thriftPluginClient._objectsStorage.Add(rowsInput);
                    _thriftPluginClient._logger.LogDebug("Added new input object '{Object}' with handle {Handle}.",
                        rowsInput.ToString(), index);
                    return Task.FromResult(new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_INPUT, index, rowsInput.ToString() ?? string.Empty),
                    });
                }
                throw new QueryCatPluginException(
                    ErrorType.INVALID_OBJECT,
                    string.Format(Resources.Errors.Object_CannotRegister, result.AsObject));
            }
            if (resultType == DataType.Blob)
            {
                var index = _thriftPluginClient._objectsStorage.Add(result.AsBlob);
                _thriftPluginClient._logger.LogDebug("Added new blob object with handle {Handle}.", index);
                return Task.FromResult(new VariantValue
                {
                    Object = new ObjectValue(ObjectType.BLOB, index, "BLOB"),
                });
            }

            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _thriftPluginClient._objectsStorage.Clean();
            _thriftPluginClient.SignalExit();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<Sdk.Column>> RowsSet_GetColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            var rowsSchema = _thriftPluginClient._objectsStorage.Get<IRowsSchema>(object_handle);
            var columns = rowsSchema.Columns.Select(SdkConvert.Convert).ToList();
            return Task.FromResult(columns);
        }

        /// <inheritdoc />
        public Task RowsSet_OpenAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                rowsInput.Open();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_CloseAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                rowsInput.Close();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_ResetAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                rowsInput.Reset();
            }
            else if (_thriftPluginClient._objectsStorage.TryGet<IRowsIterator>(object_handle, out var rowsIterator)
                && rowsIterator != null)
            {
                rowsIterator.Reset();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_SetContextAsync(int object_handle, ContextQueryInfo? context_query_info,
            CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                if (context_query_info == null)
                {
                    rowsInput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(Array.Empty<Backend.Core.Data.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage
                    );
                }
                else
                {
                    rowsInput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(
                            new List<Backend.Core.Data.Column>(),
                            context_query_info.Limit),
                        _thriftPluginClient._executionThread.ConfigStorage
                    );
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<RowsList> RowsSet_GetRowsAsync(int object_handle, int count, CancellationToken cancellationToken = default)
        {
            // Handle IRowsInput.
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                var values = new List<VariantValue>();
                var hasMore = true;
                for (var i = 0; i < count && (hasMore = rowsInput.ReadNext()); i++)
                {
                    for (var colIndex = 0; colIndex < rowsInput.Columns.Length; colIndex++)
                    {
                        if (rowsInput.ReadValue(colIndex, out var value) == ErrorCode.OK)
                        {
                            values.Add(SdkConvert.Convert(value));
                        }
                    }
                }

                var result = new RowsList(values)
                {
                    HasMore = hasMore,
                };
                return Task.FromResult(result);
            }

            // Handle IRowsIterator.
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsIterator>(object_handle, out var rowsIterator)
                && rowsIterator != null)
            {
                var values = new List<VariantValue>();
                var hasMore = true;
                for (var i = 0; i < count && (hasMore = rowsIterator.MoveNext()); i++)
                {
                    foreach (var value in rowsIterator.Current.AsArray())
                    {
                        values.Add(SdkConvert.Convert(value));
                    }
                }
                var result = new RowsList(values)
                {
                    HasMore = hasMore,
                };
                return Task.FromResult(result);
            }

            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_Invalid);
        }

        /// <inheritdoc />
        public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                var keys = rowsInput.UniqueKey.ToList();
                return Task.FromResult(keys);
            }
            return Task.FromResult(new List<string>());
        }

        /// <inheritdoc />
        public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null
                && rowsInput is IRowsInputKeys rowsInputKeys)
            {
                var result = rowsInputKeys.GetKeyColumns()
                    .Select(c => new KeyColumn(
                        c.ColumnIndex,
                        c.IsRequired,
                        c.GetOperations().Select(o => o.ToString()).ToList())
                    );
                return Task.FromResult(result.ToList());
            }
            return Task.FromResult(new List<KeyColumn>());
        }

        /// <inheritdoc />
        public Task RowsSet_SetKeyColumnValueAsync(int object_handle, int column_index, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            if (value != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null
                && rowsInput is IRowsInputKeys rowsInputKeys)
            {
                rowsInputKeys.SetKeyColumnValue(column_index, SdkConvert.Convert(value),
                    Enum.Parse<Backend.Core.Types.VariantValue.Operation>(operation));
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<bool> RowsSet_UpdateValueAsync(int object_handle, int column_index, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            if (value != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsInputUpdate>(object_handle, out var rowsInputUpdate)
                && rowsInputUpdate != null)
            {
                var result = rowsInputUpdate.UpdateValue(column_index, SdkConvert.Convert(value));
                return Task.FromResult(result == ErrorCode.OK);
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsInputUpdate)));
        }

        /// <inheritdoc />
        public Task RowsSet_WriteValueAsync(int object_handle, List<VariantValue>? values,
            CancellationToken cancellationToken = default)
        {
            if (values != null
                && _thriftPluginClient._objectsStorage.TryGet<IRowsOutput>(object_handle, out var rowsOutput)
                && rowsOutput != null)
            {
                rowsOutput.WriteValues(values.Select(SdkConvert.Convert).ToArray());
                return Task.CompletedTask;
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsOutput)));
        }

        /// <inheritdoc />
        public Task<byte[]> Blob_ReadAsync(int object_handle, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_handle, out var blobData)
                && blobData != null)
            {
                using var stream = blobData.GetStream();
                var arr = new byte[stream.Length];
                _ = stream.Read(arr, 0, arr.Length);
                return Task.FromResult(arr);
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }

        /// <inheritdoc />
        public Task<long> Blob_GetLengthAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            if (_thriftPluginClient._objectsStorage.TryGet<IBlobData>(object_handle, out var blobData)
                && blobData != null)
            {
                var length = blobData.Length;
                return Task.FromResult(length);
            }
            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
        }
    }

    private sealed class HandlerWithExceptionIntercept : Plugin.IAsync
    {
        private readonly Plugin.IAsync _handler;
        private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(Handler));

        public HandlerWithExceptionIntercept(Plugin.IAsync handler)
        {
            _handler = handler;
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
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.InitializeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.ShutdownAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex);
            }
        }

        /// <inheritdoc />
        public Task<List<Column>> RowsSet_GetColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_GetColumnsAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_OpenAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_OpenAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_CloseAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_CloseAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_ResetAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_ResetAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_SetContextAsync(int object_handle, ContextQueryInfo? context_query_info,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_SetContextAsync(object_handle, context_query_info, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<RowsList> RowsSet_GetRowsAsync(int object_handle, int count, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_GetRowsAsync(object_handle, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_GetUniqueKeyAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex,objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_GetKeyColumnsAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_SetKeyColumnValueAsync(int object_handle, int column_index, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_SetKeyColumnValueAsync(object_handle, column_index, operation, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<bool> RowsSet_UpdateValueAsync(int object_handle, int column_index, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_UpdateValueAsync(object_handle, column_index, value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task RowsSet_WriteValueAsync(int object_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.RowsSet_WriteValueAsync(object_handle, values, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<byte[]> Blob_ReadAsync(int object_handle, int offset, int count, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.Blob_ReadAsync(object_handle, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }

        /// <inheritdoc />
        public Task<long> Blob_GetLengthAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                return _handler.Blob_GetLengthAsync(object_handle, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resources.Errors.HandlerInternalError);
                throw QueryCatPluginExceptionUtils.Create(ex, objectHandle: object_handle);
            }
        }
    }
}
