using System.Globalization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Utils;

/// <summary>
/// The helper class allows to work with types system.
/// </summary>
public static class DataTypeUtils
{
    private static readonly ILogger Logger = Application.LoggerFactory.CreateLogger(typeof(DataTypeUtils));

    /// <summary>
    /// Contains the types that can be used for row column.
    /// </summary>
    internal static DataType[] RowDataTypes => new[]
    {
        DataType.Integer,
        DataType.String,
        DataType.Float,
        DataType.Integer,
        DataType.Timestamp,
        DataType.Interval,
        DataType.Boolean,
        DataType.Numeric,
        DataType.Object,
        DataType.Void,
    };

    /// <summary>
    /// Is this is a simple type (not an object).
    /// </summary>
    /// <param name="dataType">Data type.</param>
    /// <returns><c>True</c> if type is simple, <c>false</c> otherwise.</returns>
    public static bool IsSimple(DataType dataType)
        =>
            IsNumeric(dataType) || dataType == DataType.Boolean || dataType == DataType.String
                || dataType == DataType.Timestamp || dataType == DataType.Interval;

    /// <summary>
    /// Is the data type numeric.
    /// </summary>
    /// <param name="dataType">Data type to check.</param>
    /// <returns><c>True</c> if it is numeric, <c>false</c> otherwise.</returns>
    public static bool IsNumeric(DataType dataType)
        => dataType == DataType.Integer || dataType == DataType.Float || dataType == DataType.Numeric;

    public static bool EqualsWithCast(DataType dataType1, DataType dataType2)
    {
        if (IsNumeric(dataType1) && IsNumeric(dataType2))
        {
            return true;
        }
        return dataType1 == dataType2;
    }

    #region Serialization

    internal static string SerializeVariantValue(VariantValue value)
        => value.GetInternalType() switch
        {
            DataType.Null => "null",
            DataType.Void => "void",
            DataType.Integer => "i:" + value.AsIntegerUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.String => "s:" + StringUtils.Quote(value.AsStringUnsafe).ToString(),
            DataType.Boolean => "bl:" + value.AsBooleanUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Float => "fl:" + value.AsFloatUnsafe.ToString("G17", CultureInfo.InvariantCulture),
            DataType.Numeric => "n:" + value.AsNumericUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Timestamp => $"ts:{value.AsTimestampUnsafe.Ticks}:{(int)value.AsTimestampUnsafe.Kind}",
            DataType.Interval => "in:" + value.AsInterval.Ticks.ToString(CultureInfo.InvariantCulture),
            _ => string.Empty
        };

    internal static VariantValue DeserializeVariantValue(ReadOnlySpan<char> source)
    {
        if (source == "null" || source == "void")
        {
            return VariantValue.Null;
        }
        var colonIndex = source.IndexOf(':');
        if (colonIndex == -1)
        {
            Logger.LogWarning("Invalid deserialization source.");
            return VariantValue.Null;
        }

        var type = source[..colonIndex].ToString();
        var value = source[(colonIndex + 1)..];
        if (type == "i")
        {
            return new VariantValue(int.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "s")
        {
            return new VariantValue(StringUtils.Unquote(value));
        }
        if (type == "bl")
        {
            return new VariantValue(bool.Parse(value));
        }
        if (type == "fl")
        {
            return new VariantValue(float.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "n")
        {
            return new VariantValue(decimal.Parse(value, CultureInfo.InvariantCulture));
        }
        if (type == "ts")
        {
            var ticks = long.Parse(value[..^2], CultureInfo.InvariantCulture);
            var kind = (DateTimeKind)int.Parse(value[^1..], CultureInfo.InvariantCulture);
            return new VariantValue(new DateTime(ticks, kind));
        }
        if (type == "in")
        {
            var ticks = long.Parse(value, CultureInfo.InvariantCulture);
            return new VariantValue(new TimeSpan(ticks));
        }

        throw new InvalidOperationException($"Invalid deserialization type '{type}'.");
    }

    #endregion

    #region Types detection

    public static DataType DetermineTypeByValue(string value)
        => DetermineTypeByValues(new[] { value });

    internal static DataType DetermineTypeByValues(IEnumerable<string> values)
        => DetermineTypeByValues(values.Select(v => new VariantValue(v)));

    /// <summary>
    /// Try to guess optimal type by list of values. Null values are skipped.
    /// The default type is string.
    /// </summary>
    /// <param name="values">List of values.</param>
    /// <returns>Optimal type.</returns>
    internal static DataType DetermineTypeByValues(IEnumerable<VariantValue> values)
    {
        var variantValues = values
            .Where(v => !string.IsNullOrEmpty(v.AsString))
            .ToArray();

        bool TestType(DataType dataType)
        {
            var wasTested = false;
            foreach (var variantValue in variantValues)
            {
                if (string.IsNullOrEmpty(variantValue.AsString)
                    || variantValue.AsString.Equals(VariantValue.NullValueString, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                wasTested = true;
                if (!variantValue.TryCast(dataType, out _))
                {
                    return false;
                }
            }
            return wasTested;
        }

        if (TestType(DataType.Integer))
        {
            return DataType.Integer;
        }
        if (TestType(DataType.Float))
        {
            return DataType.Float;
        }
        if (TestType(DataType.Boolean))
        {
            return DataType.Boolean;
        }
        if (TestType(DataType.Timestamp))
        {
            return DataType.Timestamp;
        }

        return DataType.String;
    }

    #endregion
}
