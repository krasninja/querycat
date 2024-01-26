using System;
using QueryCat.Backend.Core;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Utilities for <see cref="QueryCatPluginException" />.
/// </summary>
public static class QueryCatPluginExceptionUtils
{
    /// <summary>
    /// Create Thrift exception wrapper from .NET exception.
    /// </summary>
    /// <param name="exception">Exception to wrap.</param>
    /// <param name="errorType">Application error type, internal by default.</param>
    /// <param name="objectHandle">Optional object handle.</param>
    /// <returns>Instance of <see cref="QueryCatPluginException" />.</returns>
    public static QueryCatPluginException Create(
        Exception exception,
        ErrorType? errorType = ErrorType.INTERNAL,
        int objectHandle = -1)
    {
        var errorTypeLocal = ErrorType.INTERNAL;
        if (!errorType.HasValue)
        {
            if (exception is QueryCatException)
            {
                errorTypeLocal = ErrorType.GENERIC;
            }
            else if (exception is ArgumentException)
            {
                errorTypeLocal = ErrorType.ARGUMENT;
            }
            else if (exception is NotSupportedException)
            {
                errorTypeLocal = ErrorType.NOT_SUPPORTED;
            }
        }
        else
        {
            errorTypeLocal = errorType.Value;
        }

        QueryCatPluginException CreateInternal(Exception ex)
        {
            return new QueryCatPluginException(errorTypeLocal, ex.Message)
            {
                ExceptionType = ex.GetType().Name,
                ObjectHandle = objectHandle,
                ExceptionStackTrace = ex.StackTrace,
            };
        }

        var qcRoot = CreateInternal(exception);
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
}
