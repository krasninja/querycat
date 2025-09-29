using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Types;

/// <summary>
/// The helper class allows to work with types system.
/// </summary>
public static class DataTypeUtils
{
    private static readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DataTypeUtils));

    /// <summary>
    /// Contains the types that can be used for row column.
    /// </summary>
    internal static DataType[] RowDataTypes =>
    [
        DataType.Integer,
        DataType.String,
        DataType.Float,
        DataType.Timestamp,
        DataType.Interval,
        DataType.Boolean,
        DataType.Numeric,
        DataType.Blob,
        DataType.Void,
        DataType.Object,
        DataType.Dynamic,
        DataType.Array,
        DataType.Map,
    ];

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

    #region Serialization

    internal static string SerializeVariantValue(VariantValue value)
        => value.Type switch
        {
            DataType.Null => VariantValue.NullValueString,
            DataType.Void => VariantValue.VoidValueString,
            DataType.Dynamic => "dynamic",
            DataType.Integer => "i:" + value.AsIntegerUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.String => "s:" + StringUtils.Quote(value.AsStringUnsafe),
            DataType.Boolean => "bl:" + value.AsBooleanUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Float => "fl:" + value.AsFloatUnsafe.ToString("G17", CultureInfo.InvariantCulture),
            DataType.Numeric => "n:" + value.AsNumericUnsafe.ToString(CultureInfo.InvariantCulture),
            DataType.Timestamp => $"ts:{value.AsTimestampUnsafe.Ticks}:{(int)value.AsTimestampUnsafe.Kind}",
            DataType.Interval => "in:" + value.AsIntervalUnsafe.Ticks.ToString(CultureInfo.InvariantCulture),
            DataType.Array => "a:" + SerializeArray(value.AsArrayUnsafe),
            DataType.Map => "m:" + SerializeMap(value.AsMapUnsafe),
            _ => string.Empty
        };

    private static string SerializeArray(IEnumerable<VariantValue> values)
    {
        var sb = new StringBuilder();
        foreach (var item in values)
        {
            var str = SerializeVariantValue(item);
            sb.Append(str.Length)
                .Append(':')
                .Append(str)
                .Append('|');
        }
        return sb.ToString();
    }

    private static string SerializeMap(IDictionary<VariantValue, VariantValue> values)
    {
        IEnumerable<VariantValue> GetKeyValuePairs(IDictionary<VariantValue, VariantValue> dict)
        {
            foreach (var item in dict)
            {
                yield return item.Key;
                yield return item.Value;
            }
        }

        return SerializeArray(GetKeyValuePairs(values));
    }

    internal static VariantValue DeserializeVariantValue(ReadOnlySpan<char> source, bool strongDeserialization = true)
    {
        if (source == VariantValue.NullValueString || source == VariantValue.VoidValueString)
        {
            return VariantValue.Null;
        }
        var colonIndex = source.IndexOf(':');
        if (colonIndex == -1)
        {
            if (!strongDeserialization)
            {
                var str = source.ToString();
                var targetType = DetermineTypeByValue(str);
                return new VariantValue(str).Cast(targetType);
            }
            _logger.LogWarning("Invalid deserialization source.");
            return VariantValue.Null;
        }

        var type = source[..colonIndex].ToString();
        var value = source[(colonIndex + 1)..];
        if (type == "i")
        {
            return new VariantValue(long.Parse(value, CultureInfo.InvariantCulture));
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
            return new VariantValue(double.Parse(value, CultureInfo.InvariantCulture));
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
        if (type == "a")
        {
            return VariantValue.CreateFromObject(DeserializeArray(value));
        }
        if (type == "m")
        {
            return VariantValue.CreateFromObject(DeserializeMap(value));
        }

        throw new InvalidOperationException(string.Format(Resources.Errors.InvalidDeserializationType, type));
    }

    private static IReadOnlyList<VariantValue> DeserializeArray(ReadOnlySpan<char> source, bool strongDeserialization = true)
    {
        var list = new List<VariantValue>();
        var startIndex = 0;
#pragma warning disable SA1002
        for (;;)
#pragma warning restore SA1002
        {
            var sizeIndex = source[startIndex..].IndexOf(':');
            if (sizeIndex == -1)
            {
                break;
            }
            sizeIndex += startIndex;
            var sizeSpan = source[startIndex..sizeIndex];
            if (sizeSpan.IsEmpty)
            {
                break;
            }
            var size = int.Parse(sizeSpan, CultureInfo.InvariantCulture);
            if (size < 1)
            {
                break;
            }
            var valueSpan = source[(sizeIndex + 1)..(sizeIndex + size + 1)];
            var value = DeserializeVariantValue(valueSpan, strongDeserialization);
            list.Add(value);
            startIndex += sizeSpan.Length + size + 2;
        }
        return list;
    }

    private static IDictionary<VariantValue, VariantValue> DeserializeMap(ReadOnlySpan<char> source, bool strongDeserialization = true)
    {
        var map = new Dictionary<VariantValue, VariantValue>();
        var arr = DeserializeArray(source, strongDeserialization);

        for (var i = 0; i < arr.Count; i++)
        {
            if (i == 0 || !int.IsOddInteger(i))
            {
                continue;
            }
            map[arr[i - 1]] = arr[i];
        }

        return map;
    }

    #endregion

    #region Types detection

    /// <summary>
    /// Try to guess optimal type by value. Null values are skipped.
    /// The default type is string.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <returns>Optimal type.</returns>
    public static DataType DetermineTypeByValue(string value) => DetermineTypeByValues([value]);

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
            .Where(v => !IsEmptyValue(in v))
            .ToArray();

        bool IsEmptyValue(in VariantValue value)
        {
            if (value.IsNull)
            {
                return true;
            }
            var type = value.Type;
            if (type == DataType.String
                && (string.IsNullOrEmpty(value.AsStringUnsafe) || value.AsStringUnsafe.Equals(VariantValue.NullValueString, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        bool TestType(DataType dataType)
        {
            var wasTested = false;
            foreach (var variantValue in variantValues)
            {
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
