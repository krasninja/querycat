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
        public override bool TryInteger(in VariantValue value, out long result)
        {
            result = value._valueUnion.IntegerValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryFloat(in VariantValue value, out double result)
        {
            result = value._valueUnion.IntegerValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryNumeric(in VariantValue value, out decimal result)
        {
            result = value._valueUnion.IntegerValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryTimestamp(in VariantValue value, out DateTime result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryInterval(in VariantValue value, out TimeSpan result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryBoolean(in VariantValue value, out bool result)
        {
            result = value._valueUnion.IntegerValue != 0;
            return true;
        }

        /// <inheritdoc />
        public override bool TryString(in VariantValue value, out string result)
        {
            result = value._valueUnion.IntegerValue.ToString(Application.Culture);
            return true;
        }

        /// <inheritdoc />
        public override bool TryBlob(in VariantValue value, out IBlobData result)
        {
            result = StreamBlobData.Empty;
            return false;
        }
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
        public override bool TryInteger(in VariantValue value, out long result)
        {
            result = (long)value._valueUnion.DoubleValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryFloat(in VariantValue value, out double result)
        {
            result = value._valueUnion.DoubleValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryNumeric(in VariantValue value, out decimal result)
        {
            result = (decimal)value._valueUnion.DoubleValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryTimestamp(in VariantValue value, out DateTime result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryInterval(in VariantValue value, out TimeSpan result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryBoolean(in VariantValue value, out bool result)
        {
            result = false;
            return false;
        }

        /// <inheritdoc />
        public override bool TryString(in VariantValue value, out string result)
        {
            result = value._valueUnion.DoubleValue.ToString(Application.Culture);
            return true;
        }

        /// <inheritdoc />
        public override bool TryBlob(in VariantValue value, out IBlobData result)
        {
            result = StreamBlobData.Empty;
            return false;
        }
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
        public override bool TryInteger(in VariantValue value, out long result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryFloat(in VariantValue value, out double result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryNumeric(in VariantValue value, out decimal result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryTimestamp(in VariantValue value, out DateTime result)
        {
            result = value._valueUnion.DateTimeValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryInterval(in VariantValue value, out TimeSpan result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryBoolean(in VariantValue value, out bool result)
        {
            result = false;
            return false;
        }

        /// <inheritdoc />
        public override bool TryString(in VariantValue value, out string result)
        {
            result = value._valueUnion.DateTimeValue.ToString(Application.Culture);
            return true;
        }

        /// <inheritdoc />
        public override bool TryBlob(in VariantValue value, out IBlobData result)
        {
            result = StreamBlobData.Empty;
            return false;
        }
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
        public override bool TryInteger(in VariantValue value, out long result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryFloat(in VariantValue value, out double result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryNumeric(in VariantValue value, out decimal result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryTimestamp(in VariantValue value, out DateTime result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryInterval(in VariantValue value, out TimeSpan result)
        {
            result = value._valueUnion.TimeSpanValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryBoolean(in VariantValue value, out bool result)
        {
            result = false;
            return false;
        }

        /// <inheritdoc />
        public override bool TryString(in VariantValue value, out string result)
        {
            result = value._valueUnion.TimeSpanValue.ToString();
            return true;
        }

        /// <inheritdoc />
        public override bool TryBlob(in VariantValue value, out IBlobData result)
        {
            result = StreamBlobData.Empty;
            return false;
        }
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
        public override bool TryInteger(in VariantValue value, out long result)
        {
            result = value._valueUnion.BooleanValue ? 1 : 0;
            return true;
        }

        /// <inheritdoc />
        public override bool TryFloat(in VariantValue value, out double result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryNumeric(in VariantValue value, out decimal result)
        {
            result = 0;
            return false;
        }

        /// <inheritdoc />
        public override bool TryTimestamp(in VariantValue value, out DateTime result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryInterval(in VariantValue value, out TimeSpan result)
        {
            result = default;
            return false;
        }

        /// <inheritdoc />
        public override bool TryBoolean(in VariantValue value, out bool result)
        {
            result = value._valueUnion.BooleanValue;
            return true;
        }

        /// <inheritdoc />
        public override bool TryString(in VariantValue value, out string result)
        {
            result = value._valueUnion.BooleanValue.ToString(Application.Culture);
            return true;
        }

        /// <inheritdoc />
        public override bool TryBlob(in VariantValue value, out IBlobData result)
        {
            result = StreamBlobData.Empty;
            return false;
        }
    }

    #endregion
}
