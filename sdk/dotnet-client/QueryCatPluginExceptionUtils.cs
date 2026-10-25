using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Utilities for <see cref="QueryCatPluginException" />.
/// </summary>
public static class QueryCatPluginExceptionUtils
{
    // Exception data keys.
    private const string KeyFunctionName = "function_name";
    private const string KeyFromType = "from_type";
    private const string KeyToType = "to_type";
    private const string KeyColumnName = "column_name";
    private const string KeyOperations = "operations";
    private const string KeySourceName = "source_name";
    private const string KeySourceType = "source_type";

    /// <summary>
    /// Create Thrift exception wrapper from .NET exception.
    /// </summary>
    /// <param name="exception">Exception to wrap.</param>
    /// <param name="errorType">Application error type, internal by default.</param>
    /// <param name="objectHandle">Optional object handle.</param>
    /// <returns>Instance of <see cref="QueryCatPluginException" />.</returns>
    public static QueryCatPluginException Create(
        Exception exception,
        ErrorType? errorType = null,
        int objectHandle = -1)
    {
        QueryCatPluginException CreateInternal(Exception ex, ErrorType? errorTypeInternal = null)
        {
            errorTypeInternal ??= GetErrorTypeByException(ex);
            var pluginException = new QueryCatPluginException(errorTypeInternal.Value, ex.Message)
            {
                ExceptionType = ex.GetType().Name,
                ObjectHandle = objectHandle,
                ExceptionStackTrace = ex.StackTrace,
            };
            var data = GetExceptionData(ex);
            if (data != null)
            {
                pluginException.Data = data;
            }
            return pluginException;
        }

        var qcRoot = CreateInternal(exception, errorType);
        var qcInner = qcRoot;

        var currentException = exception.InnerException;
        while (currentException != null)
        {
            qcInner.ExceptionNested = CreateInternal(currentException);
            qcInner = qcInner.ExceptionNested;

            currentException = currentException.InnerException;
        }

        return qcRoot;
    }

    private static ErrorType GetErrorTypeByException(Exception exception) => exception switch
    {
        AuthorizationException => ErrorType.QUERYCAT_AUTHORIZATION,
        CannotFindFunctionException => ErrorType.QUERYCAT_CANNOT_FIND_FUNCTION,
        InvalidVariantTypeException => ErrorType.QUERYCAT_INVALID_VARIANT_TYPE,
        QueryMissedConditionException => ErrorType.QUERYCAT_MISSED_CONDITION,
        QueryCatException => ErrorType.GENERIC,
        ArgumentException => ErrorType.ARGUMENT,
        NotSupportedException => ErrorType.NOT_SUPPORTED,
        _ => ErrorType.INTERNAL,
    };

    private static Dictionary<string, VariantValue>? GetExceptionData(Exception exception)
    {
        static VariantValue String(string value) => SdkConvert.Convert(new Backend.Core.Types.VariantValue(value));
        static VariantValue Integer(int value) => SdkConvert.Convert(new Backend.Core.Types.VariantValue(value));

        return exception switch
        {
            CannotFindFunctionException cannotFindFunction => new Dictionary<string, VariantValue>
            {
                [KeyFunctionName] = String(cannotFindFunction.FunctionName),
            },
            InvalidVariantTypeException invalidVariantType => new Dictionary<string, VariantValue>
            {
                [KeyFromType] = Integer((int)invalidVariantType.FromType),
                [KeyToType] = Integer((int)invalidVariantType.ToType),
            },
            QueryMissedConditionException missedCondition => new Dictionary<string, VariantValue>
            {
                [KeyColumnName] = String(missedCondition.ColumnName),
                [KeyOperations] = String(string.Join(',',
                    missedCondition.Operations.Select(o => ((int)o).ToString(CultureInfo.InvariantCulture)))),
                [KeySourceName] = String(missedCondition.InputSourceName),
                [KeySourceType] = Integer((int)missedCondition.InputSourceType),
            },
            _ => null,
        };
    }

    /// <summary>
    /// Tries to restore instance of <see cref="QueryCatException" />
    /// based on <see cref="QueryCatPluginException" /> error type and data.
    /// </summary>
    /// <param name="pluginException">Plugin exception.</param>
    /// <returns>Instance of <see cref="QueryCatException" />.</returns>
    public static QueryCatException Restore(QueryCatPluginException pluginException)
    {
        var message = pluginException.ErrorMessage ?? string.Empty;
        var data = pluginException.Data;

        string? GetString(string key)
            => data != null && data.TryGetValue(key, out var value) ? SdkConvert.Convert(value).AsString : null;

        long? GetInteger(string key)
            => data != null && data.TryGetValue(key, out var value) ? SdkConvert.Convert(value).AsInteger : null;

        switch (pluginException.Type)
        {
            case ErrorType.QUERYCAT_AUTHORIZATION:
            case ErrorType.INVALID_AUTH_TOKEN:
                return new AuthorizationException(message, pluginException);

            case ErrorType.QUERYCAT_CANNOT_FIND_FUNCTION when GetString(KeyFunctionName) is { } functionName:
                return new CannotFindFunctionException(
                    functionName,
                    new Backend.Core.Functions.FunctionCallArgumentsTypes(),
                    pluginException);

            case ErrorType.QUERYCAT_INVALID_VARIANT_TYPE
                when GetInteger(KeyFromType) is { } fromType && GetInteger(KeyToType) is { } toType:
                return new InvalidVariantTypeException(
                    (Backend.Core.Types.DataType)fromType,
                    (Backend.Core.Types.DataType)toType,
                    pluginException);

            case ErrorType.QUERYCAT_MISSED_CONDITION when GetString(KeyColumnName) is { } columnName:
                var operations = (GetString(KeyOperations) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => (Backend.Core.Types.VariantValue.Operation)int.Parse(o, CultureInfo.InvariantCulture))
                    .ToArray();
                return new QueryMissedConditionException(
                    GetString(KeySourceName) ?? string.Empty,
                    (QueryMissedConditionException.MissedConditionInputSourceType)(GetInteger(KeySourceType) ?? 0),
                    columnName,
                    operations,
                    pluginException);
        }

        // Fallback: generic exception with original message and restored nested chain.
        if (pluginException.ExceptionNested != null)
        {
            return new QueryCatException(message, Restore(pluginException.ExceptionNested));
        }
        return new QueryCatException(message, pluginException);
    }
}
