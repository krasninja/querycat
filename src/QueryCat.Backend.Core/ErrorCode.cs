namespace QueryCat.Backend.Core;

/// <summary>
/// Error codes that used while query processing.
/// </summary>
public enum ErrorCode
{
    // ReSharper disable once InconsistentNaming
    OK,
    Error,

    CannotCast,
    CannotApplyOperator,
    InvalidColumnIndex,
}
