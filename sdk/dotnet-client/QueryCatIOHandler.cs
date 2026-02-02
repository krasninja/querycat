using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Client.Remote;
using QueryCat.Plugins.Sdk;
using Column = QueryCat.Plugins.Sdk.Column;
using CursorSeekOrigin = QueryCat.Plugins.Sdk.CursorSeekOrigin;
using DataType = QueryCat.Backend.Core.Types.DataType;
using FunctionCallArguments = QueryCat.Plugins.Sdk.FunctionCallArguments;
using KeyColumn = QueryCat.Plugins.Sdk.KeyColumn;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using QuestionRequest = QueryCat.Plugins.Sdk.QuestionRequest;
using QuestionResponse = QueryCat.Plugins.Sdk.QuestionResponse;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public partial class QueryCatIOHandler : global::QueryCat.Plugins.Sdk.QueryCatIO.IAsync
{
    private readonly IExecutionThread _executionThread;
    private readonly ObjectsStorage _objectsStorage;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(QueryCatIOHandler));

    private static ObjectValue NullObjectValue { get; } = new()
    {
        Type = ObjectType.NONE,
        Handle = -1,
        Name = "NULL",
    };

    public QueryCatIOHandler(
        IExecutionThread executionThread,
        ObjectsStorage objectsStorage)
    {
        _executionThread = executionThread;
        _objectsStorage = objectsStorage;
    }

    protected QueryCat.Plugins.Sdk.VariantValue? AddObjectToStorage(QueryCat.Backend.Core.Types.VariantValue result)
    {
        var resultType = result.Type;
        if (resultType == DataType.Object || resultType == DataType.Dynamic)
        {
            if (result.AsObject is IRowsIterator rowsIterator)
            {
                var index = _objectsStorage.Add(rowsIterator);
                _logger.LogDebug("Added new iterator object '{Object}' with handle {Handle}.",
                    rowsIterator.ToString(), index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.ROWS_ITERATOR, index, rowsIterator.ToString() ?? string.Empty),
                };
            }
            else if (result.AsObject is IRowsInput rowsInput)
            {
                rowsInput.QueryContext = new PluginQueryContext(
                    new QueryContextQueryInfo(ImmutableList<Backend.Core.Data.Column>.Empty),
                    _executionThread.ConfigStorage);
                var index =_objectsStorage.Add(rowsInput);
                _logger.LogDebug("Added new input object '{Object}' with handle {Handle}.",
                    rowsInput.ToString(), index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.ROWS_INPUT, index, rowsInput.ToString() ?? string.Empty),
                };
            }
            else if (result.AsObject is IRowsOutput rowsOutput)
            {
                rowsOutput.QueryContext = new PluginQueryContext(
                    new QueryContextQueryInfo(ImmutableList<Backend.Core.Data.Column>.Empty),
                    _executionThread.ConfigStorage);
                var index =_objectsStorage.Add(rowsOutput);
                _logger.LogDebug("Added new output object '{Object}' with handle {Handle}.",
                    rowsOutput.ToString(), index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.ROWS_OUTPUT, index, rowsOutput.ToString() ?? string.Empty),
                };
            }
            else if (result.AsObject is IRowsFormatter rowsFormatter)
            {
                var index =_objectsStorage.Add(rowsFormatter);
                _logger.LogDebug("Added new formatter object '{Object}' with handle {Handle}.",
                    rowsFormatter.ToString(), index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.ROWS_FORMATTER, index, rowsFormatter.ToString() ?? string.Empty),
                };
            }
            else if (result.AsObject is IAnswerAgent answerAgent)
            {
                var index =_objectsStorage.Add(answerAgent);
                _logger.LogDebug("Added new answer agent object '{Object}' with handle {Handle}.",
                    answerAgent.ToString(), index);
                return new VariantValue
                {
                    Object = new ObjectValue(ObjectType.ANSWER_AGENT, index, answerAgent.ToString() ?? string.Empty),
                };
            }
            throw new QueryCatPluginException(
                ErrorType.INVALID_OBJECT,
                string.Format(Resources.Errors.Object_CannotRegister, result.AsObject));
        }
        if (resultType == DataType.Blob)
        {
            var index = _objectsStorage.Add(result.AsBlobUnsafe);
            _logger.LogDebug("Added new blob object with handle {Handle}.", index);
            return new VariantValue
            {
                Object = new ObjectValue(ObjectType.BLOB, index, "BLOB"),
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<VariantValue> CallFunctionAsync(long token, string function_name, FunctionCallArguments? call_args, int object_handle,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(CallFunctionAsync), cancellationToken);
        call_args ??= new FunctionCallArguments();

        var func = _executionThread.FunctionsManager.FindByNameFirst(function_name);
        var frame =_executionThread.Stack.CreateFrame();
        try
        {
            // Format frame.
            var positionalIndex = 0;
            var positional = call_args.Positional ?? new List<VariantValue>();
            var named = call_args.Named ?? new Dictionary<string, VariantValue>();

            foreach (var funcArgument in func.Arguments)
            {
                if (positional.Count >= positionalIndex + 1)
                {
                    frame.Push(SdkConvert.Convert(positional[positionalIndex++]));
                    continue;
                }

                if (named.TryGetValue(funcArgument.Name, out var value))
                {
                    frame.Push(SdkConvert.Convert(value));
                }
                else
                {
                    frame.Push(funcArgument.DefaultValue);
                }
            }

            // In PluginFunction we cannot determine arguments, so push positional and named as is.
            if (func.Arguments.Length == 0)
            {
                foreach (var value in positional)
                {
                    frame.Push(SdkConvert.Convert(value));
                }
                foreach (var value in named)
                {
                    frame.Push(SdkConvert.Convert(value.Value));
                }
            }

            var result = await FunctionCaller.CallAsync(
                func.Delegate,
                _executionThread,
                cancellationToken);

            var resultObject = AddObjectToStorage(result);
            if (resultObject != null)
            {
                return resultObject;
            }

            return SdkConvert.Convert(result);
        }
        finally
        {
            frame.Dispose();
        }
    }

    /// <inheritdoc />
    public virtual async Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Blob_ReadAsync), cancellationToken);
        if (_objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
            && blobData != null)
        {
            await using var stream = blobData.GetStream();
            if (offset >= stream.Length)
            {
                return [];
            }
            if (offset > 0)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            var buffer = new byte[stream.Length];
            var readBytes = await stream.ReadAsync(buffer, 0, count, cancellationToken);
            if (readBytes != buffer.Length)
            {
                buffer = buffer.AsSpan(0, readBytes).ToArray();
            }
            return buffer;
        }
        throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
    }

    /// <inheritdoc />
    public virtual async Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Blob_WriteAsync), cancellationToken);
        if (_objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
            && blobData != null)
        {
            await using var stream = blobData.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            return bytes.Length;
        }
        throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
    }

    /// <inheritdoc />
    public virtual async Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Blob_GetLengthAsync), cancellationToken);
        if (_objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
            && blobData != null)
        {
            var length = blobData.Length;
            return length;
        }
        throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
    }

    /// <inheritdoc />
    public virtual async Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Blob_GetContentTypeAsync), cancellationToken);
        if (_objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
            && blobData != null)
        {
            return blobData.ContentType;
        }
        throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
    }

    /// <inheritdoc />
    public virtual async Task<string> Blob_GetNameAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Blob_GetNameAsync), cancellationToken);
        if (_objectsStorage.TryGet<IBlobData>(object_blob_handle, out var blobData)
            && blobData != null)
        {
            return blobData.Name;
        }
        throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_IsNotBlob);
    }

    /// <inheritdoc />
    public virtual async Task<List<Column>> RowsSet_GetColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_GetColumnsAsync), cancellationToken);
        var rowsSchema = _objectsStorage.Get<IRowsSchema>(object_rows_set_handle);
        var columns = rowsSchema.Columns.Select(SdkConvert.Convert).ToList();
        return columns;
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_OpenAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_OpenAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            await rowsSource.OpenAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_CloseAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_CloseAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            await rowsSource.CloseAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_ResetAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_ResetAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            await rowsSource.ResetAsync(cancellationToken);
        }
        else if (_objectsStorage.TryGet<IRowsIterator>(object_rows_set_handle, out var rowsIterator)
                 && rowsIterator != null)
        {
            await rowsIterator.ResetAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_SetContextAsync(long token, int object_rows_set_handle, ContextQueryInfo? context_query_info,
        ContextInfo? context_info, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_SetContextAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsSource>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            if (context_query_info == null)
            {
                rowsSource.QueryContext = new PluginQueryContext(
                    new QueryContextQueryInfo([]),
                    _executionThread.ConfigStorage
                );
            }
            else
            {
                var columns = context_query_info.Columns
                              ?? (IList<QueryCat.Plugins.Sdk.Column>)ImmutableList<QueryCat.Plugins.Sdk.Column>.Empty;
                rowsSource.QueryContext = new PluginQueryContext(
                    new QueryContextQueryInfo(
                        columns.Select(SdkConvert.Convert).ToList(),
                        context_query_info.Limit)
                    {
                        Offset = context_query_info.Offset,
                    },
                    _executionThread.ConfigStorage
                );
            }
            if (context_info != null)
            {
                rowsSource.QueryContext.PrereadRowsCount = context_info.PrereadRowsCount;
                rowsSource.QueryContext.SkipIfNoColumns =  context_info.SkipIfNoColumns;
            }
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> RowsSet_PositionAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_PositionAsync), cancellationToken);
        if (_objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            return rowsSource.Position;
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
        return -1;
    }

    /// <inheritdoc />
    public virtual async Task<int> RowsSet_TotalRowsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_TotalRowsAsync), cancellationToken);
        if (_objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            return rowsSource.TotalRows;
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
        return -1;
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_SeekAsync(long token, int object_rows_set_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_SeekAsync), cancellationToken);
        if (_objectsStorage.TryGet<ICursorRowsIterator>(object_rows_set_handle, out var rowsSource)
            && rowsSource != null)
        {
            rowsSource.Seek(offset, SdkConvert.Convert(origin));
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
    }

    /// <inheritdoc />
    public virtual async Task<RowsList> RowsSet_GetRowsAsync(long token, int object_rows_set_handle, int count,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_GetRowsAsync), cancellationToken);

        // Handle IRowsInput.
        if (_objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
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
        if (_objectsStorage.TryGet<IRowsIterator>(object_rows_set_handle, out var rowsIterator)
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
    public virtual async Task<List<string>> RowsSet_GetUniqueKeyAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_GetUniqueKeyAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
            && rowsInput != null)
        {
            return rowsInput.UniqueKey.ToList();
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
        return [];
    }

    /// <inheritdoc />
    public virtual async Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_GetKeyColumnsAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
            && rowsInput != null)
        {
            var result = rowsInput.GetKeyColumns()
                .Select(c => new KeyColumn(
                    c.ColumnIndex,
                    c.IsRequired,
                    c.GetOperations().Select(o => o.ToString()).ToList())
                );
            return result.ToList();
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
        return [];
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_SetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
        VariantValue? value, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_SetKeyColumnValueAsync), cancellationToken);
        if (value != null
            && _objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
            && rowsInput != null)
        {
            rowsInput.SetKeyColumnValue(column_index, SdkConvert.Convert(value),
                Enum.Parse<Backend.Core.Types.VariantValue.Operation>(operation));
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
    }

    /// <inheritdoc />
    public virtual async Task RowsSet_UnsetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_UnsetKeyColumnValueAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsInput>(object_rows_set_handle, out var rowsInput)
            && rowsInput != null)
        {
            rowsInput.UnsetKeyColumnValue(column_index, Enum.Parse<Backend.Core.Types.VariantValue.Operation>(operation));
        }
        else
        {
            LogCannotFindObject(object_rows_set_handle);
        }
    }

    /// <inheritdoc />
    public virtual async Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(long token, int object_rows_set_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_UpdateValueAsync), cancellationToken);
        if (value != null
            && _objectsStorage.TryGet<IRowsInputUpdate>(object_rows_set_handle, out var rowsInputUpdate)
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
    public virtual async Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(long token, int object_rows_set_handle, List<VariantValue>? values,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_WriteValuesAsync), cancellationToken);
        if (values != null
            && _objectsStorage.TryGet<IRowsOutput>(object_rows_set_handle, out var rowsOutput)
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
    public virtual async Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_DeleteRowAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsInputDelete>(object_rows_set_handle, out var rowsInputDelete)
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
    public virtual async Task<ModelDescription> RowsSet_GetDescriptionAsync(long token, int object_handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsSet_GetDescriptionAsync), cancellationToken);
        if (_objectsStorage.TryGet<IModelDescription>(object_handle, out var model)
            && model != null)
        {
            return SdkConvert.Convert(model);
        }
        return new ModelDescription(string.Empty, string.Empty);
    }

    /// <inheritdoc />
    public virtual async Task<int> RowsFormatter_OpenInputAsync(long token, int object_rows_formatter_handle, int object_blob_handle, string? key,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsFormatter_OpenInputAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsFormatter>(object_rows_formatter_handle, out var rowsFormatter)
            && rowsFormatter != null)
        {
            var remoteBlob = new ThriftRemoteBlobProxy(
                new SimpleThriftSessionProvider(this),
                object_blob_handle,
                token);
            var rowsInput = rowsFormatter.OpenInput(remoteBlob, key);
            var index =_objectsStorage.Add(rowsInput);
            _logger.LogDebug("Added new input object '{Object}' with handle {Handle}.",
                rowsInput.ToString(), index);
            return index;
        }
        throw new QueryCatPluginException(
            ErrorType.INVALID_OBJECT,
            string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsFormatter)));
    }

    /// <inheritdoc />
    public virtual async Task<int> RowsFormatter_OpenOutputAsync(long token, int object_rows_formatter_handle, int object_blob_handle,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(RowsFormatter_OpenOutputAsync), cancellationToken);
        if (_objectsStorage.TryGet<IRowsFormatter>(object_rows_formatter_handle, out var rowsFormatter)
            && rowsFormatter != null)
        {
            var remoteBlob = new ThriftRemoteBlobProxy(
                new SimpleThriftSessionProvider(this),
                object_blob_handle,
                token);
            var rowsOutput = rowsFormatter.OpenOutput(remoteBlob);
            var index =_objectsStorage.Add(rowsOutput);
            _logger.LogDebug("Added new output object '{Object}' with handle {Handle}.",
                rowsOutput.ToString(), index);
            return index;
        }
        throw new QueryCatPluginException(
            ErrorType.INVALID_OBJECT,
            string.Format(Resources.Errors.Object_InvalidType, typeof(IRowsFormatter)));
    }

    /// <inheritdoc />
    public virtual async Task<QuestionResponse> AnswerAgent_AskAsync(long token, int object_answer_agent_handle, QuestionRequest? request,
        CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(AnswerAgent_AskAsync), cancellationToken);
        if (request != null
            && _objectsStorage.TryGet<IAnswerAgent>(object_answer_agent_handle, out var answerAgent)
            && answerAgent != null)
        {
            var response = await answerAgent.AskAsync(SdkConvert.Convert(request), cancellationToken);
            return SdkConvert.Convert(response);
        }
        throw new QueryCatPluginException(
            ErrorType.INVALID_OBJECT,
            string.Format(Resources.Errors.Object_InvalidType, typeof(IAnswerAgent)));
    }

    /// <inheritdoc />
    public async Task Thread_CloseHandleAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Thread_CloseHandleAsync), cancellationToken);
        _objectsStorage.Remove(handle);
    }

    /// <inheritdoc />
    public async Task<ObjectValue> Thread_GetHandleInfoAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Thread_GetHandleInfoAsync), cancellationToken);
        if (_objectsStorage.TryGet<object>(handle, out var obj))
        {
            if (obj is IRowsIterator)
            {
                return new ObjectValue(ObjectType.ROWS_ITERATOR, handle, obj.ToString() ?? string.Empty);
            }
            if (obj is IRowsInput)
            {
                return new ObjectValue(ObjectType.ROWS_INPUT, handle, obj.ToString() ?? string.Empty);
            }
            if (obj is IRowsOutput)
            {
                return new ObjectValue(ObjectType.ROWS_OUTPUT, handle, obj.ToString() ?? string.Empty);
            }
            if (obj is IRowsFormatter)
            {
                return new ObjectValue(ObjectType.ROWS_FORMATTER, handle, obj.ToString() ?? string.Empty);
            }
            if (obj is IAnswerAgent)
            {
                return new ObjectValue(ObjectType.ANSWER_AGENT, handle, obj.ToString() ?? string.Empty);
            }
            if (obj is IBlobData blobData)
            {
                return new ObjectValue(ObjectType.BLOB, handle, blobData.Name);
            }
        }

        return NullObjectValue;
    }

    /// <inheritdoc />
    public async Task<ObjectValue> Thread_GetHandleFromVariableAsync(long token, string name, CancellationToken cancellationToken = default)
    {
        await BeforeCallAsync(token, nameof(Thread_GetHandleFromVariableAsync), cancellationToken);
        if (_executionThread.TopScope.TryGetVariable(name, out var variable))
        {
            var objValue = AddObjectToStorage(variable);
            if (objValue != null && objValue.Object != null)
            {
                return objValue.Object;
            }
        }

        return NullObjectValue;
    }

    protected virtual Task BeforeCallAsync(long token, string methodName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Warning, "Cannot find object with handle {ObjectHandle}.")]
    protected partial void LogCannotFindObject(int objectHandle);
}
