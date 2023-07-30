using System;
using QueryCat.Plugins.Sdk;

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

    public static VariantValue Convert(QueryCat.Backend.Types.VariantValue value)
    {
        var type = value.GetInternalType();
        return type switch
        {
            Backend.Types.DataType.Void => new VariantValue
            {
                IsNull = true,
            },
            Backend.Types.DataType.Null => new VariantValue
            {
                IsNull = true,
            },
            Backend.Types.DataType.Integer => new VariantValue
            {
                Integer = value.AsIntegerUnsafe,
            },
            Backend.Types.DataType.String => new VariantValue
            {
                String = value.AsStringUnsafe,
            },
            Backend.Types.DataType.Float => new VariantValue
            {
                Float = value.AsFloatUnsafe,
            },
            Backend.Types.DataType.Timestamp => new VariantValue
            {
                Timestamp = (long)(value.AsTimestampUnsafe - UnixEpoch).TotalSeconds,
            },
            Backend.Types.DataType.Boolean => new VariantValue
            {
                Boolean = value.AsBooleanUnsafe,
            },
            Backend.Types.DataType.Numeric => new VariantValue
            {
                Decimal = Convert(value.AsNumericUnsafe),
            },
            Backend.Types.DataType.Interval => new VariantValue
            {
                Interval = (long)value.AsIntervalUnsafe.TotalMilliseconds,
            },
            Backend.Types.DataType.Object => new VariantValue
            {
                Object = new ObjectValue
                {
                    Id = (int)value.AsIntegerUnsafe,
                    Name = value.AsObjectUnsafe?.GetType().Name ?? string.Empty,
                }
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static Backend.Types.VariantValue Convert(VariantValue value)
    {
        if (value.__isset.isNull)
        {
            return Backend.Types.VariantValue.Null;
        }
        if (value.__isset.integer)
        {
            return new Backend.Types.VariantValue(value.Integer);
        }
        if (value.__isset.@string)
        {
            return new Backend.Types.VariantValue(value.String);
        }
        if (value.__isset.@float)
        {
            return new Backend.Types.VariantValue(value.Float);
        }
        if (value.__isset.timestamp)
        {
            return new Backend.Types.VariantValue(UnixEpoch.AddSeconds(value.Timestamp));
        }
        if (value.__isset.boolean)
        {
            return new Backend.Types.VariantValue(value.Boolean);
        }
        if (value.__isset.@decimal)
        {
            return new Backend.Types.VariantValue(Convert(value.Decimal!));
        }
        if (value.__isset.interval)
        {
            return new Backend.Types.VariantValue(new TimeSpan(0, 0, 0, 0, (int)value.Interval));
        }
        throw new ArgumentOutOfRangeException(nameof(value));
    }

    public static Backend.Types.DataType Convert(Sdk.DataType type)
    {
        return type switch
        {
            DataType.NULL => Backend.Types.DataType.Null,
            DataType.INTEGER => Backend.Types.DataType.Integer,
            DataType.STRING => Backend.Types.DataType.String,
            DataType.FLOAT => Backend.Types.DataType.Float,
            DataType.TIMESTAMP => Backend.Types.DataType.Timestamp,
            DataType.BOOLEAN => Backend.Types.DataType.Boolean,
            DataType.NUMERIC => Backend.Types.DataType.Numeric,
            DataType.INTERVAL => Backend.Types.DataType.Interval,
            DataType.OBJECT => Backend.Types.DataType.Object,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static DataType Convert(Backend.Types.DataType type)
    {
        return type switch
        {
            Backend.Types.DataType.Null => DataType.NULL,
            Backend.Types.DataType.Integer => DataType.INTEGER,
            Backend.Types.DataType.String => DataType.STRING,
            Backend.Types.DataType.Float => DataType.FLOAT,
            Backend.Types.DataType.Timestamp => DataType.TIMESTAMP,
            Backend.Types.DataType.Boolean => DataType.BOOLEAN,
            Backend.Types.DataType.Numeric => DataType.NUMERIC,
            Backend.Types.DataType.Interval => DataType.INTERVAL,
            Backend.Types.DataType.Object => DataType.OBJECT,
            Backend.Types.DataType.Void => DataType.NULL,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static Column Convert(QueryCat.Backend.Relational.Column column)
    {
        return new Column(column.Name, Convert(column.DataType))
        {
            Description = column.Description
        };
    }

    public static QueryCat.Backend.Relational.Column Convert(Column column)
    {
        return new QueryCat.Backend.Relational.Column(column.Name, Convert(column.Type))
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
}
