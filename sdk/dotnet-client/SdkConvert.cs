using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
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
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
                Timestamp = (long)(value.AsTimestampUnsafe - UnixEpoch).TotalSeconds,
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

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
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
                Json = jsonNode.ToJsonString(JsonSerializerOptions),
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
            return new Backend.Core.Types.VariantValue(UnixEpoch.AddSeconds(value.Timestamp));
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
            DataType.INTEGER => Backend.Core.Types.DataType.Integer,
            DataType.STRING => Backend.Core.Types.DataType.String,
            DataType.FLOAT => Backend.Core.Types.DataType.Float,
            DataType.TIMESTAMP => Backend.Core.Types.DataType.Timestamp,
            DataType.BOOLEAN => Backend.Core.Types.DataType.Boolean,
            DataType.NUMERIC => Backend.Core.Types.DataType.Numeric,
            DataType.INTERVAL => Backend.Core.Types.DataType.Interval,
            DataType.OBJECT => Backend.Core.Types.DataType.Object,
            DataType.BLOB => Backend.Core.Types.DataType.Blob,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
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
            Backend.Core.Types.DataType.Void => DataType.NULL,
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
            QueryCatErrorCode.CANNOT_CAST => ErrorCode.CannotCast,
            QueryCatErrorCode.CANNOT_APPLY_OPERATOR => ErrorCode.CannotApplyOperator,
            QueryCatErrorCode.INVALID_COLUMN_INDEX => ErrorCode.InvalidColumnIndex,
            QueryCatErrorCode.INVALID_INPUT_STATE => ErrorCode.InvalidInputState,
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, Resources.Errors.InvalidErrorCode),
        };
}
