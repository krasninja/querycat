namespace QueryCat.Backend.Core;

/// <summary>
/// Error codes that used while query processing.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// No error.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    OK = 0,

    /// <summary>
    /// General error.
    /// </summary>
    Error = 1,

    /// <summary>
    /// The row was deleted.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// No data available.
    /// </summary>
    NoData = 3,

    /// <summary>
    /// Not supported operation.
    /// </summary>
    NotSupported = 4,

    /// <summary>
    /// Not initialized or not opened (closed).
    /// </summary>
    NotInitialized = 5,

    // More specific errors.

    /// <summary>
    /// Cannot cast to the specified type.
    /// </summary>
    CannotCast = 100,

    /// <summary>
    /// Cannot apply operator to the specified value type.
    /// </summary>
    CannotApplyOperator = 101,

    /// <summary>
    /// Incorrect column index, out of range.
    /// </summary>
    InvalidColumnIndex = 102,

    /// <summary>
    /// Rows input instance in inconsistent state.
    /// </summary>
    InvalidInputState = 103,
}
