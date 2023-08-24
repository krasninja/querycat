namespace QueryCat.Backend.Abstractions.Functions;

/// <summary>
/// The interface describes window information if function is run within window clause.
/// </summary>
public interface IWindowInfo
{
    /// <summary>
    /// Total rows in current window.
    /// </summary>
    /// <returns>Total rows.</returns>
    long GetTotalRows();

    /// <summary>
    /// Get current row position in the window.
    /// </summary>
    /// <returns>Zero based row position.</returns>
    long GetCurrentRowPosition();
}
