using System.Globalization;

namespace QueryCat.Backend.Core.Types;

public partial struct VariantValue
{
    #region Integer

    internal sealed class IntegerDataTypeObject : DataTypeObject
    {
        public static IntegerDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private IntegerDataTypeObject() : base(DataType.Integer)
        {
        }

        /// <inheritdoc />
        public override bool CanToInteger => true;

        /// <inheritdoc />
        public override long? ToInteger(in VariantValue value) => value._valueUnion.IntegerValue;

        /// <inheritdoc />
        public override bool CanToFloat => true;

        /// <inheritdoc />
        public override double? ToFloat(in VariantValue value) => value._valueUnion.IntegerValue;

        /// <inheritdoc />
        public override bool CanToNumeric => true;

        /// <inheritdoc />
        public override decimal? ToNumeric(in VariantValue value) => value._valueUnion.IntegerValue;

        /// <inheritdoc />
        public override bool CanToBoolean => true;

        /// <inheritdoc />
        public override bool? ToBoolean(in VariantValue value) => value._valueUnion.IntegerValue != 0;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsIntegerUnsafe.ToString(Application.Culture);
    }

    #endregion

    #region Float

    internal sealed class FloatDataTypeObject : DataTypeObject
    {
        public static FloatDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private FloatDataTypeObject() : base(DataType.Float)
        {
        }

        /// <inheritdoc />
        public override bool CanToInteger => true;

        /// <inheritdoc />
        public override long? ToInteger(in VariantValue value) => (long)value._valueUnion.DoubleValue;

        /// <inheritdoc />
        public override bool CanToFloat => true;

        /// <inheritdoc />
        public override double? ToFloat(in VariantValue value) => value._valueUnion.DoubleValue;

        /// <inheritdoc />
        public override bool CanToNumeric => true;

        /// <inheritdoc />
        public override decimal? ToNumeric(in VariantValue value) => (decimal)value._valueUnion.DoubleValue;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsFloatUnsafe.ToString(FloatNumberFormat, Application.Culture);
    }

    #endregion

    #region Timestamp

    internal sealed class TimestampDataTypeObject : DataTypeObject
    {
        public static TimestampDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private TimestampDataTypeObject() : base(DataType.Timestamp)
        {
        }

        /// <inheritdoc />
        public override bool CanToTimestamp => true;

        /// <inheritdoc />
        public override DateTime? ToTimestamp(in VariantValue value) => value._valueUnion.DateTimeValue;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsTimestampUnsafe.ToString(Application.Culture);
    }

    #endregion

    #region Interval

    internal sealed class IntervalDataTypeObject : DataTypeObject
    {
        public static IntervalDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private IntervalDataTypeObject() : base(DataType.Interval)
        {
        }

        /// <inheritdoc />
        public override bool CanToInterval => true;

        /// <inheritdoc />
        public override TimeSpan? ToInterval(in VariantValue value) => value._valueUnion.TimeSpanValue;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsIntervalUnsafe.ToString();
    }

    #endregion

    #region Boolean

    internal sealed class BooleanDataTypeObject : DataTypeObject
    {
        public static BooleanDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private BooleanDataTypeObject() : base(DataType.Boolean)
        {
        }

        /// <inheritdoc />
        public override bool CanToInteger => true;

        /// <inheritdoc />
        public override long? ToInteger(in VariantValue value) => value._valueUnion.BooleanValue ? 1 : 0;

        /// <inheritdoc />
        public override bool CanToBoolean => true;

        /// <inheritdoc />
        public override bool? ToBoolean(in VariantValue value) => value._valueUnion.BooleanValue;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsTimestampUnsafe.ToString(Application.Culture);
    }

    #endregion

    #region String

    internal sealed class StringDataTypeObject : DataTypeObject
    {
        public static StringDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private StringDataTypeObject() : base(DataType.String)
        {
        }

        #region Integer

        /// <inheritdoc />
        public override bool CanToInteger => true;

        /// <inheritdoc />
        public override long? ToInteger(in VariantValue value) => StringToInteger(value.AsStringUnsafe, out _);

        internal static long? StringToInteger(in ReadOnlySpan<char> value, out bool success)
        {
            success = long.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
            if (!success)
            {
                // Try HEX format.
                success = value.StartsWith("0x") &&
                          long.TryParse(value[2..], NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier,
                              Application.Culture, out @out);
            }
            return success ? @out : null;
        }

        #endregion

        #region Float

        /// <inheritdoc />
        public override bool CanToFloat => true;

        /// <inheritdoc />
        public override double? ToFloat(in VariantValue value) => StringToFloat(value.AsStringUnsafe, out _);

        internal static double? StringToFloat(string? value, out bool success)
            => StringToFloat((ReadOnlySpan<char>)value, out success);

        internal static double? StringToFloat(in ReadOnlySpan<char> value, out bool success)
        {
            success = double.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
            return success ? @out : null;
        }

        #endregion

        #region Numberic

        /// <inheritdoc />
        public override bool CanToNumeric => true;

        /// <inheritdoc />
        public override decimal? ToNumeric(in VariantValue value) => StringToNumeric(value.AsStringUnsafe, out _);

        internal static decimal? StringToNumeric(in string? value, out bool success)
            => StringToNumeric((ReadOnlySpan<char>)value, out success);

        internal static decimal? StringToNumeric(in ReadOnlySpan<char> value, out bool success)
        {
            success = decimal.TryParse(value, NumberStyles.Any, Application.Culture, out var @out);
            return success ? @out : null;
        }

        #endregion

        #region Boolean

        /// <inheritdoc />
        public override bool CanToBoolean => true;

        /// <inheritdoc />
        public override bool? ToBoolean(in VariantValue value) => StringToBoolean(value.AsStringUnsafe, out _);

        internal static bool? StringToBoolean(string? value, out bool success)
            => StringToBoolean((ReadOnlySpan<char>)value, out success);

        internal static bool? StringToBoolean(in ReadOnlySpan<char> value, out bool success)
        {
            if (value.IsEmpty)
            {
                success = false;
                return Null;
            }

            if (value.Equals(TrueValueString, StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("YES", StringComparison.OrdinalIgnoreCase)
                || value.Equals("ON", StringComparison.OrdinalIgnoreCase)
                || value.Equals("T", StringComparison.OrdinalIgnoreCase))
            {
                success = true;
                return new VariantValue(true);
            }

            if (value.Equals(FalseValueString, StringComparison.OrdinalIgnoreCase)
                || value.Equals("0", StringComparison.OrdinalIgnoreCase)
                || value.Equals("NO", StringComparison.OrdinalIgnoreCase)
                || value.Equals("OFF", StringComparison.OrdinalIgnoreCase)
                || value.Equals("F", StringComparison.OrdinalIgnoreCase))
            {
                success = true;
                return new VariantValue(false);
            }

            success = false;
            return Null;
        }

        #endregion

        #region String

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsStringUnsafe;

        internal static string StringToString(in ReadOnlySpan<char> value, out bool success)
        {
            success = true;
            return !value.IsEmpty ? value.ToString() : string.Empty;
        }

        #endregion

        #region Interval

        /// <inheritdoc />
        public override bool CanToInterval => true;

        /// <inheritdoc />
        public override TimeSpan? ToInterval(in VariantValue value) => StringToInterval(value.AsStringUnsafe, out _);

        internal static TimeSpan? StringToInterval(in ReadOnlySpan<char> value, out bool success)
        {
            success = IntervalParser.TryParseInterval(value.ToString(), out var @out);
            return success ? @out : null;
        }

        #endregion

        #region Timestamp

        /// <inheritdoc />
        public override bool CanToTimestamp => true;

        /// <inheritdoc />
        public override DateTime? ToTimestamp(in VariantValue value) => StringToTimestamp(value.AsStringUnsafe, out _);

        internal static DateTime? StringToTimestamp(string? value, out bool success)
            => StringToTimestamp((ReadOnlySpan<char>)value, out success);

        private static readonly string[] _dateTimeAdditionalFormats =
        {
            "yyMMdd",
            "yyyyMMdd"
        };

        internal static DateTime? StringToTimestamp(in ReadOnlySpan<char> value, out bool success)
        {
            success = DateTime.TryParse(value, Application.Culture, DateTimeStyles.None, out var @out);
            if (!success)
            {
                success = DateTime.TryParseExact(value, _dateTimeAdditionalFormats, null,
                    DateTimeStyles.AllowWhiteSpaces, out @out);
            }
            return success ? @out : null;
        }

        #endregion

        #region Blob

        /// <inheritdoc />
        public override IBlobData ToBlob(in VariantValue value) => StringToBlob(value.AsStringUnsafe, out _);

        internal static IBlobData StringToBlob(in string? value, out bool success)
        {
            success = true;
            var bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
            return new StreamBlobData(bytes, "text/plain");
        }

        #endregion
    }

    #endregion

    #region Numeric

    internal sealed class NumericDataTypeObject : DataTypeObject
    {
        public static NumericDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private NumericDataTypeObject() : base(DataType.Numeric)
        {
        }

        /// <inheritdoc />
        public override bool CanToInteger => true;

        /// <inheritdoc />
        public override long? ToInteger(in VariantValue value) => (long?)value.AsNumericUnsafe;

        /// <inheritdoc />
        public override bool CanToFloat => true;

        /// <inheritdoc />
        public override double? ToFloat(in VariantValue value) => (double?)value.AsNumericUnsafe;

        /// <inheritdoc />
        public override bool CanToNumeric => true;

        /// <inheritdoc />
        public override decimal? ToNumeric(in VariantValue value) => value.AsNumericUnsafe;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsNumericUnsafe.ToString(FloatNumberFormat, Application.Culture);
    }

    #endregion

    #region Blob

    internal sealed class BlobDataTypeObject : DataTypeObject
    {
        public static BlobDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private BlobDataTypeObject() : base(DataType.Blob)
        {
        }

        /// <inheritdoc />
        public override bool CanToBlob => true;

        /// <inheritdoc />
        public override IBlobData ToBlob(in VariantValue value) => value.AsBlobUnsafe;

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => BlobToString(value.AsBlobUnsafe, out _);

        internal static string BlobToString(IBlobData? value, out bool success)
        {
            success = true;
            value ??= StreamBlobData.Empty;
            var sr = new StreamReader(value.GetStream());
            return sr.ReadToEnd();
        }
    }

    #endregion

    #region Object

    internal sealed class ObjectDataTypeObject : DataTypeObject
    {
        public static ObjectDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private ObjectDataTypeObject() : base(DataType.Object)
        {
        }

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsObjectUnsafe?.ToString() ?? string.Empty;
    }

    #endregion

    #region Array

    internal sealed class ArrayDataTypeObject : DataTypeObject
    {
        public static ArrayDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private ArrayDataTypeObject() : base(DataType.Array)
        {
        }

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsObjectUnsafe?.ToString() ?? string.Empty;
    }

    #endregion

    #region Map

    internal sealed class MapDataTypeObject : DataTypeObject
    {
        public static MapDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private MapDataTypeObject() : base(DataType.Map)
        {
        }

        /// <inheritdoc />
        public override bool CanToString => true;

        /// <inheritdoc />
        public override string ToString(in VariantValue value) => value.AsObjectUnsafe?.ToString() ?? string.Empty;
    }

    #endregion

    #region Null

    internal sealed class NullDataTypeObject : DataTypeObject
    {
        public static NullDataTypeObject Instance { get; } = new();

        /// <inheritdoc />
        private NullDataTypeObject() : base(DataType.Boolean)
        {
        }
    }

    #endregion
}
