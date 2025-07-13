using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Plugins.Sdk;
using Column = QueryCat.Plugins.Sdk.Column;
using DataType = QueryCat.Plugins.Sdk.DataType;
using VariantValue = QueryCat.Plugins.Sdk.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The class allows to convert to/from SDK and native QueryCat types.
/// </summary>
public static class SdkConvert
{
    /// <summary>
    /// Unix epoch.
    /// </summary>
    private static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static VariantValue Convert(Backend.Core.Types.VariantValue value)
    {
        return value.Type switch
        {
            Backend.Core.Types.DataType.Void => new VariantValue
            {
                IsNull = true,
            },
            Backend.Core.Types.DataType.Null => new VariantValue
            {
                IsNull = true,
            },
            Backend.Core.Types.DataType.Integer => new VariantValue
            {
                Integer = value.AsIntegerUnsafe,
            },
            Backend.Core.Types.DataType.String => new VariantValue
            {
                String = value.AsStringUnsafe,
            },
            Backend.Core.Types.DataType.Float => new VariantValue
            {
                Float = value.AsFloatUnsafe,
            },
            Backend.Core.Types.DataType.Timestamp => new VariantValue
            {
                Timestamp = (long)(value.AsTimestampUnsafe - _unixEpoch).TotalSeconds,
            },
            Backend.Core.Types.DataType.Boolean => new VariantValue
            {
                Boolean = value.AsBooleanUnsafe,
            },
            Backend.Core.Types.DataType.Numeric => new VariantValue
            {
                Decimal = Convert(value.AsNumericUnsafe),
            },
            Backend.Core.Types.DataType.Interval => new VariantValue
            {
                Interval = (long)value.AsIntervalUnsafe.TotalMilliseconds,
            },
            Backend.Core.Types.DataType.Object or Backend.Core.Types.DataType.Blob => ConvertObject(value),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
    };

    private static VariantValue ConvertObject(Backend.Core.Types.VariantValue value)
    {
        if (value.AsObjectUnsafe == null)
        {
            return new VariantValue
            {
                Object = new ObjectValue
                {
                    Handle = 0,
                    Name = QueryCat.Backend.Core.Types.VariantValue.NullValueString,
                    Type = ObjectType.GENERIC,
                }
            };
        }

        // JSON.
        if (value.AsObjectUnsafe is JsonNode jsonNode)
        {
            return new VariantValue
            {
                Json = jsonNode.ToJsonString(_jsonSerializerOptions),
            };
        }

        var type = ObjectType.GENERIC;
        if (value.AsObjectUnsafe is IBlobData)
        {
            type = ObjectType.BLOB;
        }
        else if (value.AsObjectUnsafe is IRowsInput)
        {
            type = ObjectType.BLOB;
        }
        else if (value.AsObjectUnsafe is IRowsIterator)
        {
            type = ObjectType.ROWS_ITERATOR;
        }
        else if (value.AsObjectUnsafe is IRowsOutput)
        {
            type = ObjectType.ROWS_OUTPUT;
        }
        return new VariantValue
        {
            Object = new ObjectValue
            {
                Handle = (int)value.AsIntegerUnsafe,
                Name = value.AsObjectUnsafe.GetType().Name,
                Type = type,
            }
        };
    }

    public static Backend.Core.Types.VariantValue Convert(VariantValue? value)
    {
        if (value == null)
        {
            return Backend.Core.Types.VariantValue.Null;
        }
        if (value.__isset.isNull)
        {
            return Backend.Core.Types.VariantValue.Null;
        }
        if (value.__isset.integer)
        {
            return new Backend.Core.Types.VariantValue(value.Integer);
        }
        if (value.__isset.@string)
        {
            return new Backend.Core.Types.VariantValue(value.String);
        }
        if (value.__isset.@float)
        {
            return new Backend.Core.Types.VariantValue(value.Float);
        }
        if (value.__isset.timestamp)
        {
            return new Backend.Core.Types.VariantValue(_unixEpoch.AddSeconds(value.Timestamp));
        }
        if (value.__isset.boolean)
        {
            return new Backend.Core.Types.VariantValue(value.Boolean);
        }
        if (value.__isset.@decimal)
        {
            return new Backend.Core.Types.VariantValue(Convert(value.Decimal!));
        }
        if (value.__isset.interval)
        {
            return new Backend.Core.Types.VariantValue(new TimeSpan(0, 0, 0, 0, (int)value.Interval));
        }
        if (value.__isset.@object && value.Object != null)
        {
            return Backend.Core.Types.VariantValue.CreateFromObject(
                new RemoteObject(value.Object.Handle, value.Object.Type.ToString()));
        }
        if (value.__isset.json && value.Json != null)
        {
            return Backend.Core.Types.VariantValue.CreateFromObject(JsonNode.Parse(value.Json));
        }
        throw new ArgumentOutOfRangeException(nameof(value));
    }

    public static Backend.Core.Types.DataType Convert(DataType type)
    {
        return type switch
        {
            DataType.NULL => Backend.Core.Types.DataType.Null,
            DataType.VOID => Backend.Core.Types.DataType.Void,
            DataType.INTEGER => Backend.Core.Types.DataType.Integer,
            DataType.STRING => Backend.Core.Types.DataType.String,
            DataType.FLOAT => Backend.Core.Types.DataType.Float,
            DataType.TIMESTAMP => Backend.Core.Types.DataType.Timestamp,
            DataType.BOOLEAN => Backend.Core.Types.DataType.Boolean,
            DataType.NUMERIC => Backend.Core.Types.DataType.Numeric,
            DataType.INTERVAL => Backend.Core.Types.DataType.Interval,
            DataType.BLOB => Backend.Core.Types.DataType.Blob,
            DataType.OBJECT => Backend.Core.Types.DataType.Object,
            DataType.DYNAMIC => Backend.Core.Types.DataType.Dynamic,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    public static DataType Convert(Backend.Core.Types.DataType type)
    {
        return type switch
        {
            Backend.Core.Types.DataType.Null => DataType.NULL,
            Backend.Core.Types.DataType.Integer => DataType.INTEGER,
            Backend.Core.Types.DataType.String => DataType.STRING,
            Backend.Core.Types.DataType.Float => DataType.FLOAT,
            Backend.Core.Types.DataType.Timestamp => DataType.TIMESTAMP,
            Backend.Core.Types.DataType.Boolean => DataType.BOOLEAN,
            Backend.Core.Types.DataType.Numeric => DataType.NUMERIC,
            Backend.Core.Types.DataType.Interval => DataType.INTERVAL,
            Backend.Core.Types.DataType.Object => DataType.OBJECT,
            Backend.Core.Types.DataType.Blob => DataType.BLOB,
            Backend.Core.Types.DataType.Void => DataType.VOID,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static Column Convert(Backend.Core.Data.Column column)
    {
        return new Column(column.Name, Convert(column.DataType))
        {
            Description = column.Description
        };
    }

    public static Backend.Core.Data.Column Convert(Column column)
    {
        return new Backend.Core.Data.Column(column.Name, Convert(column.Type))
        {
            Description = column.Description ?? string.Empty,
        };
    }

    // Source:
    // https://learn.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/protobuf-data-types#creating-a-custom-decimal-type-for-protobuf

    private const decimal NanoFactor = 1_000_000_000;

    public static decimal Convert(DecimalValue value) => value.Units + value.Nanos / NanoFactor;

    public static DecimalValue Convert(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue
        {
            Units = units,
            Nanos = nanos
        };
    }

    public static PluginData Convert(Assembly? assembly)
    {
        if (assembly == null)
        {
            return new PluginData();
        }
        var assemblyName = assembly.GetName();
        return new PluginData
        {
            Name = assemblyName.Name ?? string.Empty,
            Version = assemblyName.Version?.ToString() ?? "0.0.0",
        };
    }

    public static QueryCatErrorCode Convert(ErrorCode errorCode)
        => errorCode switch
        {
            ErrorCode.OK => QueryCatErrorCode.OK,
            ErrorCode.Error => QueryCatErrorCode.ERROR,
            ErrorCode.Deleted => QueryCatErrorCode.DELETED,
            ErrorCode.NoData => QueryCatErrorCode.NO_DATA,
            ErrorCode.NotSupported => QueryCatErrorCode.NOT_SUPPORTED,
            ErrorCode.NotInitialized => QueryCatErrorCode.NOT_INITIALIZED,
            ErrorCode.AccessDenied => QueryCatErrorCode.ACCESS_DENIED,
            ErrorCode.InvalidArguments => QueryCatErrorCode.INVALID_ARGUMENTS,
            ErrorCode.Aborted => QueryCatErrorCode.ABORTED,
            ErrorCode.Closed => QueryCatErrorCode.CLOSED,
            ErrorCode.NoAction => QueryCatErrorCode.NO_ACTION,
            ErrorCode.CannotCast => QueryCatErrorCode.CANNOT_CAST,
            ErrorCode.CannotApplyOperator => QueryCatErrorCode.CANNOT_APPLY_OPERATOR,
            ErrorCode.InvalidColumnIndex => QueryCatErrorCode.INVALID_COLUMN_INDEX,
            ErrorCode.InvalidInputState => QueryCatErrorCode.INVALID_INPUT_STATE,
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, Resources.Errors.InvalidErrorCode),
        };

    public static ErrorCode Convert(QueryCatErrorCode errorCode)
        => errorCode switch
        {
            QueryCatErrorCode.OK => ErrorCode.OK,
            QueryCatErrorCode.ERROR => ErrorCode.Error,
            QueryCatErrorCode.DELETED => ErrorCode.Deleted,
            QueryCatErrorCode.NO_DATA => ErrorCode.NoData,
            QueryCatErrorCode.NOT_SUPPORTED => ErrorCode.NotSupported,
            QueryCatErrorCode.NOT_INITIALIZED => ErrorCode.NotInitialized,
            QueryCatErrorCode.ACCESS_DENIED => ErrorCode.AccessDenied,
            QueryCatErrorCode.INVALID_ARGUMENTS => ErrorCode.InvalidArguments,
            QueryCatErrorCode.ABORTED => ErrorCode.Aborted,
            QueryCatErrorCode.CLOSED => ErrorCode.Closed,
            QueryCatErrorCode.NO_ACTION => ErrorCode.NoAction,
            QueryCatErrorCode.CANNOT_CAST => ErrorCode.CannotCast,
            QueryCatErrorCode.CANNOT_APPLY_OPERATOR => ErrorCode.CannotApplyOperator,
            QueryCatErrorCode.INVALID_COLUMN_INDEX => ErrorCode.InvalidColumnIndex,
            QueryCatErrorCode.INVALID_INPUT_STATE => ErrorCode.InvalidInputState,
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, Resources.Errors.InvalidErrorCode),
        };

    public static Sdk.CursorSeekOrigin Convert(Backend.Core.Data.CursorSeekOrigin origin)
        => origin switch
        {
            Backend.Core.Data.CursorSeekOrigin.Begin => Sdk.CursorSeekOrigin.BEGIN,
            Backend.Core.Data.CursorSeekOrigin.Current => Sdk.CursorSeekOrigin.CURRENT,
            Backend.Core.Data.CursorSeekOrigin.End => Sdk.CursorSeekOrigin.END,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

    public static Backend.Core.Data.CursorSeekOrigin Convert(Sdk.CursorSeekOrigin origin)
        => origin switch
        {
            Sdk.CursorSeekOrigin.BEGIN => Backend.Core.Data.CursorSeekOrigin.Begin,
            Sdk.CursorSeekOrigin.CURRENT => Backend.Core.Data.CursorSeekOrigin.Current,
            Sdk.CursorSeekOrigin.END => Backend.Core.Data.CursorSeekOrigin.End,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

    public static Sdk.LogLevel Convert(Microsoft.Extensions.Logging.LogLevel origin)
        => origin switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => Sdk.LogLevel.TRACE,
            Microsoft.Extensions.Logging.LogLevel.Debug => Sdk.LogLevel.DEBUG,
            Microsoft.Extensions.Logging.LogLevel.Information => Sdk.LogLevel.INFORMATION,
            Microsoft.Extensions.Logging.LogLevel.Warning => Sdk.LogLevel.WARNING,
            Microsoft.Extensions.Logging.LogLevel.Error => Sdk.LogLevel.ERROR,
            Microsoft.Extensions.Logging.LogLevel.Critical => Sdk.LogLevel.CRITICAL,
            Microsoft.Extensions.Logging.LogLevel.None => Sdk.LogLevel.NONE,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

    public static Microsoft.Extensions.Logging.LogLevel Convert(Sdk.LogLevel origin)
        => origin switch
        {
            LogLevel.TRACE => Microsoft.Extensions.Logging.LogLevel.Trace,
            LogLevel.DEBUG => Microsoft.Extensions.Logging.LogLevel.Debug,
            LogLevel.INFORMATION => Microsoft.Extensions.Logging.LogLevel.Information,
            LogLevel.WARNING => Microsoft.Extensions.Logging.LogLevel.Warning,
            LogLevel.ERROR => Microsoft.Extensions.Logging.LogLevel.Error,
            LogLevel.CRITICAL => Microsoft.Extensions.Logging.LogLevel.Critical,
            LogLevel.NONE => Microsoft.Extensions.Logging.LogLevel.None,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

    public static Backend.Core.Execution.CompletionItemKind Convert(Sdk.CompletionKind kind)
        => kind switch
        {
            CompletionKind.MISC => CompletionItemKind.Misc,
            CompletionKind.KEYWORD => CompletionItemKind.Keyword,
            CompletionKind.FUNCTION => CompletionItemKind.Function,
            CompletionKind.VARIABLE => CompletionItemKind.Variable,
            CompletionKind.PROPERTY => CompletionItemKind.Property,
            CompletionKind.TEXT => CompletionItemKind.Text,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public static Sdk.CompletionKind Convert(Backend.Core.Execution.CompletionItemKind kind)
        => kind switch
        {
            CompletionItemKind.Misc => CompletionKind.MISC,
            CompletionItemKind.Keyword => CompletionKind.KEYWORD,
            CompletionItemKind.Function => CompletionKind.FUNCTION,
            CompletionItemKind.Variable => CompletionKind.VARIABLE,
            CompletionItemKind.Property => CompletionKind.PROPERTY,
            CompletionItemKind.Text => CompletionKind.TEXT,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public static Backend.Core.Execution.CompletionTextEdit Convert(Sdk.CompletionTextEdit edit)
        => new(edit.Start, edit.End, edit.NewText);

    public static Sdk.CompletionTextEdit Convert(Backend.Core.Execution.CompletionTextEdit edit)
        => new(edit.Start, edit.End, edit.NewText);

    public static Backend.Core.Execution.CompletionResult Convert(Sdk.CompletionResult result)
        => new(
            result.Label,
            Convert(result.Kind),
            result.Documentation,
            (float)result.Relevance,
            (result.Edits ?? []) .Select(Convert).ToArray()
        );

    public static Sdk.CompletionResult Convert(Backend.Core.Execution.CompletionResult result)
        => new(
            Convert(result.Completion.Kind),
            result.Completion.Label,
            result.Completion.Documentation,
            result.Completion.Relevance,
            result.Edits.Select(Convert).ToList()
        );

    public static Backend.Core.Execution.ExecutionStatistic.RowErrorInfo Convert(Sdk.StatisticRowError rowError)
        => new(SdkConvert.Convert(rowError.ErrorCode), rowError.RowIndex, rowError.ColumnIndex, rowError.Value);

    public static Sdk.StatisticRowError Convert(Backend.Core.Execution.ExecutionStatistic.RowErrorInfo rowError)
        => new(SdkConvert.Convert(rowError.ErrorCode), rowError.RowIndex, rowError.ColumnIndex)
        {
            Value = rowError.Value,
        };

    public static Backend.Core.Execution.ExecutionStatistic Convert(Sdk.Statistic statistic)
        => new ThriftPluginStatistic();

    public static Sdk.Statistic Convert(Backend.Core.Execution.ExecutionStatistic statistic)
    {
        return new Statistic(
            (long)statistic.ExecutionTime.TotalMilliseconds,
            statistic.ProcessedCount,
            statistic.ErrorsCount,
            statistic.Errors.Select(SdkConvert.Convert).ToList()
        );
    }
}
