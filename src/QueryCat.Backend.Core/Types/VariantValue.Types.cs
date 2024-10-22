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
    }

    #endregion
}
