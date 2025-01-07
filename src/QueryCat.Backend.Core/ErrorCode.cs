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

    /// <summary>
    /// Requires more permissions for the action.
    /// </summary>
    AccessDenied = 6,

    /// <summary>
    /// Invalid number of arguments or incorrect call.
    /// </summary>
    InvalidArguments = 7,

    /// <summary>
    /// Operation aborted.
    /// </summary>
    Aborted = 8,

    /// <summary>
    /// The object is closed.
    /// </summary>
    Closed = 9,

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
