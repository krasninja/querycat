using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Plugins.Sdk;
using DataType = QueryCat.Backend.Types.DataType;
using KeyColumn = QueryCat.Plugins.Sdk.KeyColumn;

namespace QueryCat.Plugins.Client;

public partial class ThriftPluginClient
{
    private class Handler : Plugin.IAsync
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
            var callInfo = new FunctionCallInfo(_thriftPluginClient._executionThread);
            callInfo.FunctionName = func.Name;
            foreach (var arg in args)
            {
                callInfo.Push(SdkConvert.Convert(arg));
            }
            var result = func.Delegate.Invoke(callInfo);
            var resultType = result.GetInternalType();
            if (resultType == DataType.Object)
            {
                if (result.AsObject is IRowsIterator rowsIterator)
                {
                    var index = _thriftPluginClient._objectsStorage.Add(rowsIterator);
                    _thriftPluginClient._logger.LogDebug("Added new object '{Object}' with handle {Handle}.",
                        rowsIterator.ToString(), index);
                    return Task.FromResult(new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_ITERATOR, index, rowsIterator.ToString() ?? string.Empty),
                    });
                }
                else if (result.AsObject is IRowsInput rowsInput)
                {
                    rowsInput.QueryContext = new PluginQueryContext(
                        new QueryContextQueryInfo(new List<QueryCat.Backend.Relational.Column>()),
                        _thriftPluginClient._executionThread.ConfigStorage);
                    var index =_thriftPluginClient._objectsStorage.Add(rowsInput);
                    _thriftPluginClient._logger.LogDebug("Added new object '{Object}' with handle {Handle}.",
                        rowsInput.ToString(), index);
                    return Task.FromResult(new VariantValue
                    {
                        Object = new ObjectValue(ObjectType.ROWS_INPUT, index, rowsInput.ToString() ?? string.Empty),
                    });
                }
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, $"Cannot register object '{result.AsObject}'.");
            }

            return Task.FromResult(SdkConvert.Convert(result));
        }

        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _thriftPluginClient._objectsStorage.Clean();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<Column>> RowsSet_GetColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            var rowsSchema = _thriftPluginClient._objectsStorage.Get<IRowsSchema>(object_handle);
            try
            {
                var columns = rowsSchema.Columns.Select(SdkConvert.Convert).ToList();
                return Task.FromResult(columns);
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
        public Task RowsSet_OpenAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null)
                {
                    rowsInput.Open();
                }
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_CloseAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null)
                {
                    rowsInput.Close();
                }
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_ResetAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
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
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RowsSet_SetContextAsync(int object_handle, ContextQueryInfo? context_query_info,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null)
                {
                    if (context_query_info == null)
                    {
                        rowsInput.QueryContext = new PluginQueryContext(
                            new QueryContextQueryInfo(Array.Empty<QueryCat.Backend.Relational.Column>()),
                            _thriftPluginClient._executionThread.ConfigStorage
                        );
                    }
                    else
                    {
                        rowsInput.QueryContext = new PluginQueryContext(
                            new QueryContextQueryInfo(
                                new List<QueryCat.Backend.Relational.Column>(),
                                context_query_info.Limit),
                            _thriftPluginClient._executionThread.ConfigStorage
                        );
                    }
                }
                return Task.CompletedTask;
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
        public Task<RowsList> RowsSet_GetRowsAsync(int object_handle, int count, CancellationToken cancellationToken = default)
        {
            // Handle IRowsInput.
            if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                && rowsInput != null)
            {
                var values = new List<VariantValue>();
                var hasMore = rowsInput.ReadNext();
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
                var hasMore = rowsIterator.MoveNext();
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

            throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, "Invalid object.");
        }

        /// <inheritdoc />
        public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null)
                {
                    var keys = rowsInput.UniqueKey.ToList();
                    return Task.FromResult(keys);
                }
                return Task.FromResult(new List<string>());
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
        public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null
                    && rowsInput is IRowsInputKeys rowsInputKeys)
                {
                    var result = rowsInputKeys.GetKeyColumns()
                        .Select(c => new KeyColumn(
                            c.ColumnName,
                            c.IsRequired,
                            c.Operations.Select(o => o.ToString()).ToList())
                        );
                    return Task.FromResult(result.ToList());
                }
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }

            return Task.FromResult(new List<KeyColumn>());
        }

        /// <inheritdoc />
        public Task RowsSet_SetKeyColumnValueAsync(int object_handle, string column_name, string operation, VariantValue? value,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (value != null
                    && _thriftPluginClient._objectsStorage.TryGet<IRowsInput>(object_handle, out var rowsInput)
                    && rowsInput != null
                    && rowsInput is IRowsInputKeys rowsInputKeys)
                {
                    rowsInputKeys.SetKeyColumnValue(column_name, SdkConvert.Convert(value),
                        Enum.Parse<QueryCat.Backend.Types.VariantValue.Operation>(operation));
                }
            }
            catch (QueryCatException ex)
            {
                throw new QueryCatPluginException(ErrorType.GENERIC, ex.Message)
                {
                    ObjectHandle = object_handle,
                };
            }
            return Task.CompletedTask;
        }
    }
}
